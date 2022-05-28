using Anotar.Serilog;
using Dharma_DSharp.Constants;
using Dharma_DSharp.Modules.Dharma;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using static Dharma_DSharp.Constants.DharmaConstants;
using System.Xml.Linq;

namespace Dharma_DSharp
{
    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            var discordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = TryGetToken(args),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                LargeThreshold = 300,
                LoggerFactory = CreateSerilogLoggerFactory()
            });
            CreateCommandExtensions(discordClient);

            MainAsync(discordClient).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(DiscordClient discordClient)
        {
            await TryConnectDiscordBot(discordClient).ConfigureAwait(false);
            HookEventListeners(discordClient);

            await Task.Delay(-1).ConfigureAwait(false);
        }

        private static async Task TryConnectDiscordBot(DiscordClient discordClient)
        {
            try
            {
                await discordClient.ConnectAsync().ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogTo.Error(ex, "Unauthorized, bad token? Pass it as first argument to the console application");
                Environment.Exit(128);
            }
            catch (BadRequestException ex)
            {
                LogTo.Error(ex, "Request is malformed, probably need to update dsharp library?");
                Environment.Exit(128);
            }
            catch (ServerErrorException ex)
            {
                LogTo.Error(ex, "Internal server error of discord api, wait a bit or maybe update dsharp library?");
                Environment.Exit(128);
            }
        }

        /// <summary>
        /// Hooks various event listeners of the <paramref name="discordClient"/> to logic.
        /// </summary>
        private static void HookEventListeners(DiscordClient discordClient)
        {
            // TODO: Use for leveling system
            discordClient.MessageCreated += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    if (e.Guild.Id != GuildId || e.Channel.Id != ChannelIds.PhantasyNgsAlert || e.Message.Embeds.FirstOrDefault() == null)
                    {
                        return;
                    }

                    var searchString = "is happening in ";
                    var indexOfHappening = e.Message.Embeds[0].Description.IndexOf(searchString);
                    if (indexOfHappening == -1 || indexOfHappening == 0)
                    {
                        return;
                    }
                    var cutIrrelevant = e.Message.Embeds[0].Description.Substring(indexOfHappening + searchString.Length);
                    var cuttedIrrelevant = cutIrrelevant.Substring(0, cutIrrelevant.IndexOf(' '));

                    var partyChannel = await client.GetChannelAsync(ChannelIds.PartyingChannel).ConfigureAwait(false);
                    if (partyChannel == null)
                    {
                        return;
                    }

                    var openIndex = e.Message.Embeds[0].Description.IndexOf('(') + 1;
                    var closeIndex = e.Message.Embeds[0].Description.IndexOf(')');
                    var uqName = e.Message.Embeds[0].Description.Substring(openIndex, closeIndex - openIndex);

                    var registerButton = new DiscordButtonComponent(ButtonStyle.Success, $"register_button_{uqName}", "Party up");
                    var leaveButton = new DiscordButtonComponent(ButtonStyle.Danger, $"leave_button_{uqName}", "Leave");
                    var msg = new DiscordMessageBuilder()
                        .WithEmbed(PartyingEmbed(uqName + " is happening in " + cuttedIrrelevant + " minutes!", e.Message.Embeds[0].Image.Url.ToString()))
                        .AddComponents(new DiscordComponent[] { registerButton, leaveButton });
                    var message = await partyChannel.SendMessageAsync(msg).ConfigureAwait(false);

                    // Will wait for 40ish minutes
                    var _ = await message.CollectReactionsAsync();
                    await message.DeleteAsync("The uq is over").ConfigureAwait(false);
                });

                return Task.CompletedTask;
            };

            discordClient.ComponentInteractionCreated += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    var uqName = string.Empty;
                    if (e.Message.Embeds.FirstOrDefault() is not DiscordEmbed)
                    {
                        return;
                    }
                    uqName = e.Message.Embeds[0].Title.Substring(e.Message.Embeds[0].Title.IndexOf(' '));
                    var registerButton = new DiscordButtonComponent(ButtonStyle.Success, $"register_button_{uqName}", "Party up");
                    var leaveButton = new DiscordButtonComponent(ButtonStyle.Danger, $"leave_button_{uqName}", "Leave");

                    if (e.Id.Contains("register"))
                    {
                        if (e.Message.Embeds[0].Description.Contains(e.User.Username))
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                new DiscordInteractionResponseBuilder()
                                    .AddEmbed(e.Message.Embeds[0])
                                    .AddComponents(new DiscordComponent[] { registerButton, leaveButton }));
                            return;
                        }

                        var indexEmptyUser = e.Message.Embeds[0].Description.IndexOf(". \n") + 2;
                        if (indexEmptyUser == -1)
                        {
                            // Update embed and remove join button
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                new DiscordInteractionResponseBuilder()
                                    .AddEmbed(e.Message.Embeds[0])
                                    .AddComponents(leaveButton));

                            // Add new embed
                            var msg = new DiscordMessageBuilder()
                                .WithEmbed(PartyingEmbed(e.Message.Embeds[0].Title, e.Message.Embeds[0].Thumbnail.Url.ToString()))
                                .AddComponents(new DiscordComponent[] { registerButton, leaveButton });
                            var message = await e.Channel.SendMessageAsync(msg).ConfigureAwait(false);

                            // Will wait for 40ish minutes
                            var _ = await message.CollectReactionsAsync();
                            await message.DeleteAsync("The uq is over").ConfigureAwait(false);
                            return;
                        }

                        var withInsertedUser = e.Message.Embeds[0].Description.Substring(0, indexEmptyUser) + e.User.Username + e.Message.Embeds[0].Description.Substring(indexEmptyUser);
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                            new DiscordInteractionResponseBuilder()
                                .AddEmbed(new DiscordEmbedBuilder()
                                    .WithTitle(e.Message.Embeds[0].Title)
                                    .WithThumbnail(e.Message.Embeds[0].Thumbnail.Url.ToString())
                                    .WithDescription(withInsertedUser))
                                .AddComponents(new DiscordComponent[] { registerButton, leaveButton }));
                        return;
                    }
                    else if (e.Id.Contains("leave"))
                    {
                        var indexOfUser = e.Message.Embeds[0].Description.IndexOf(e.User.Username);
                        if (indexOfUser == -1 || indexOfUser == 0)
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                new DiscordInteractionResponseBuilder()
                                    .AddEmbed(e.Message.Embeds[0])
                                    .AddComponents(new DiscordComponent[] { registerButton, leaveButton }));
                            return;
                        }
                        var description = e.Message.Embeds[0].Description;

                        var withRemovedUser = description.Substring(0, indexOfUser) + description.Substring(indexOfUser + e.User.Username.Length);
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                            new DiscordInteractionResponseBuilder()
                                .AddEmbed(new DiscordEmbedBuilder()
                                    .WithTitle(e.Message.Embeds[0].Title)
                                    .WithThumbnail(e.Message.Embeds[0].Thumbnail.Url.ToString())
                                    .WithDescription(withRemovedUser))
                                .AddComponents(new DiscordComponent[] { registerButton, leaveButton }));
                        return;
                    }

                    return;
                });

                return Task.CompletedTask;
            };

            // Used for welcome-embed
            discordClient.GuildMemberAdded += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    if (e.Guild.Id != GuildId)
                    {
                        return;
                    }

                    var welcomeDescription = Strings.WelcomeDescription.Replace("{0}", e.Member.Id.ToString());
                    await SendEmbedToWelcomeHall(client,
                        welcomeDescription,
                        new DiscordColor(Strings.WelcomeEmbedColor),
                        Strings.WelcomeTitle,
                        Strings.WelcomeThumbnailUrl,
                        Strings.WelcomeFooterImage).ConfigureAwait(false);
                });

                return Task.CompletedTask;
            };

            // Used for membership screening
            discordClient.GuildMemberUpdated += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    // If the user already has a role the screening process will be skipped.
                    if (e.RolesBefore.Count != 0 || e.Guild.Id != GuildId)
                    {
                        return;
                    }

                    // Already passed membership screening
                    if (e.PendingBefore.HasValue && !e.PendingBefore.Value)
                    {
                        return;
                    }

                    var homieRole = e.Guild.Roles.FirstOrDefault(role => role.Key == RoleIds.HomieId);
                    if (homieRole.Value == default)
                    {
                        return;
                    }

                    if (e.PendingAfter.HasValue && !e.PendingAfter.Value)
                    {
                        // Passed membership screening grant homie

                        LogTo.Debug("{user} passed membership screening, granting {homie}", e.Member.Username, homieRole);
                        await e.Member.GrantRoleAsync(homieRole.Value, "Granted by Dharma Bot membership screening addition").ConfigureAwait(false);
                    }
                });
                return Task.CompletedTask;
            };

            // Used for kick/ban/leave message
            discordClient.GuildMemberRemoved += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    if (e.Guild.Id != GuildId)
                    {
                        return;
                    }

                    var auditLog = e.Guild.GetAuditLogsAsync(2).Result;
                    var kickLogs = auditLog.Where(singleLog => singleLog.ActionType == AuditLogActionType.Kick)?.FirstOrDefault(log => ((DiscordAuditLogKickEntry)log).Target.Id == e.Member.Id);
                    var banLogs = auditLog.Where(singleLog => singleLog.ActionType == AuditLogActionType.Ban)?.FirstOrDefault(log => ((DiscordAuditLogBanEntry)log).Target.Id == e.Member.Id);

                    if (banLogs != null)
                    {
                        var bannedDescription = Strings.UserBannedDescription.Replace("{0}", e.Member.Id.ToString());
                        await SendEmbedToWelcomeHall(client,
                            bannedDescription,
                            new DiscordColor(Strings.UserBannedEmbedColor),
                            Strings.UserBannedTitle,
                            Strings.UserBannedThumbnailUrl,
                            Strings.UserBannedFooterImage).ConfigureAwait(false);
                        return;
                    }

                    if (kickLogs != null)
                    {
                        var kickedDescription = Strings.UserKickedDescription.Replace("{0}", e.Member.Id.ToString());
                        await SendEmbedToWelcomeHall(client,
                            kickedDescription,
                            new DiscordColor(Strings.UserKickedEmbedColor),
                            Strings.UserKickedTitle,
                            Strings.UserKickedThumbnailUrl,
                            Strings.UserKickedFooterImage).ConfigureAwait(false);
                        return;
                    }

                    // User left
                    var leftDescription = Strings.UserLeftDescription.Replace("{0}", e.Member.Id.ToString());
                    await SendEmbedToWelcomeHall(client,
                        leftDescription,
                        new DiscordColor(Strings.UserLeftEmbedColor),
                        Strings.UserLeftTitle,
                        Strings.UserLeftThumbnailUrl,
                        Strings.UserLeftFooterImage).ConfigureAwait(false);
                    return;
                });

                return Task.CompletedTask;
            };
        }

        private static DiscordEmbed PartyingEmbed(string title, string imageUrl)
        {
            return new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithThumbnail(imageUrl)
                .WithDescription("Group 1:\n1. \n2. \n3. \n4. \n\nGroup2:\n5. \n6. \n7. \n8. \n");
        }

        /// <param name="discordClient">Client that is used to send the embed.</param>
        /// <param name="description">Description of the embed.</param>
        /// <param name="discordColor">The color of the embed.</param>
        /// <param name="title">Title of the embed.</param>
        /// <param name="topRightCornerThumbnail">Provide a valid uri.</param>
        /// <param name="footerImage">Provide a valid uri.</param>
        private async static Task SendEmbedToWelcomeHall(DiscordClient discordClient,
            string description,
            DiscordColor discordColor,
            string title = "",
            string topRightCornerThumbnail = "",
            string footerImage = "")
        {
            var welcomeHall = await discordClient.GetChannelAsync(DharmaConstants.ChannelIds.WelcomeHall);
            description = description.Replace(@"\n", "\n");
            var embed = new DiscordEmbedBuilder()
                .WithDescription(description)
                .WithColor(discordColor)
                .WithFooter("The Dharma Team");

            if (!string.IsNullOrEmpty(title))
            {
                embed.WithTitle(title);
            }

            if (!string.IsNullOrEmpty(topRightCornerThumbnail))
            {
                embed.WithThumbnail(topRightCornerThumbnail);
            }

            if (!string.IsNullOrEmpty(footerImage))
            {
                embed.WithImageUrl(footerImage);
            }

            await welcomeHall.SendMessageAsync(embed).ConfigureAwait(false);
            return;
        }

        /// <summary>
        /// Creates the <see cref="ServiceProvider"/> and the <see cref="SlashCommandsExtension"/>.
        /// </summary>
        private static void CreateCommandExtensions(DiscordClient discordClient)
        {
            var serviceProvider = CreateDependencyInjection(discordClient);

            discordClient.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromMinutes(40)
            });

            var slash = discordClient.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = serviceProvider
            });

            // Register all command classes here
            slash.RegisterCommands<GrantCommands>(GuildId);
            slash.RegisterCommands<ListMembersWithRoleCommand>(GuildId);
        }

        /// <summary>
        /// Creates the <see cref="ServiceProvider"/> that will handle dependency injection for the command extensions, e.g. <see cref="SlashCommandsExtension"/>.
        /// </summary>
        private static ServiceProvider? CreateDependencyInjection(DiscordClient discordClient)
        {
            return new ServiceCollection()
                .AddSingleton<Random>()
                .BuildServiceProvider();
        }

        /// <summary>
        /// Creates the <see cref="ILoggerFactory"/> that will be used for logging throughtout the project.
        /// Thankfully <see cref="DSharpPlus"/> supports all logging frameworks that implement the logging abstractions
        /// provided by microsoft.
        /// </summary>
        private static ILoggerFactory CreateSerilogLoggerFactory()
        {
            // Add serilog console sink
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            return new LoggerFactory().AddSerilog();
        }

        /// <summary>
        /// Get token from token-file in debug or as command line argument in any other non-debug configuration.
        /// May exit the environment if no valid token is given.
        /// </summary>
        /// <param name="args"></param>
        private static string TryGetToken(string[] args)
        {
            var token = string.Empty;
            try
            {
#if DEBUG
                token = File.ReadAllText(args[0]);
#else
                token = args[0];
#endif
                if (string.IsNullOrEmpty(token))
                {
                    throw new ArgumentNullException("Token can't be null/zero/empty.");
                }

                return token;
            }
            catch (Exception e)
            {
#if DEBUG
                LogTo.Information(e, "Error while trying to read token, did you pass it as a text-file to the application?");
#else
                LogTo.Information(e, "Error while trying to read token, did you pass it as first argument to the application?");
#endif
                Environment.Exit(128);
                return token;
            }
        }
    }
}
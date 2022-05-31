using Anotar.Serilog;
using DSharpPlus.Exceptions;
using DSharpPlus;
using DSharpPlus.Entities;
using static Dharma_DSharp.Constants.DharmaConstants;
using Dharma_DSharp.Constants;
using DSharpPlus.Interactivity.Extensions;
using Dharma_DSharp.Modules.Dharma;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.EventArgs;
using Dharma_DSharp.Extensions;

namespace Dharma_DSharp
{
    public static class DiscordController
    {
        public static async Task TryConnectDiscordBot(DiscordClient discordClient)
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
        /// Creates the <see cref="ServiceProvider"/> and the <see cref="SlashCommandsExtension"/>.
        /// </summary>
        public static void CreateCommandExtensions(DiscordClient discordClient)
        {
            var serviceProvider = new ServiceCollection()
                .BuildServiceProvider();

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
        /// Hooks various event listeners of the <paramref name="discordClient"/> to logic.
        /// </summary>
        public static void HookEventListeners(DiscordClient discordClient)
        {
            /// TODO: Add leveling system with message created event
            discordClient.MessageCreated += CheckForPhantasyStarAlert;
            discordClient.ComponentInteractionCreated += RegisterOrDeregisterPartyMember;
            discordClient.GuildMemberAdded += PostWelcomeEmbed;
            discordClient.GuildMemberUpdated += MembershipScreeningRoleGrant;
            discordClient.GuildMemberRemoved += PostKickLeaveBanEmbed;
        }

        /// <summary>
        /// Currently used for scanning the phantasy-ngs-alert channel for new uq alerts.
        /// If a uq or concert is found it will post an announcement like embed in the partying channel.
        /// </summary>
        private static Task CheckForPhantasyStarAlert(DiscordClient client, MessageCreateEventArgs e)
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
                var unixTimeStamp = DateTimeOffset.UtcNow.RoundUpToNearest30().ToUnixTimeSeconds();
                var uqTime = $"<t:{unixTimeStamp}:f>";

                var partyChannel = await client.GetChannelAsync(ChannelIds.PartyingChannel).ConfigureAwait(false);
                if (partyChannel == null)
                {
                    return;
                }

                var openIndex = e.Message.Embeds[0].Description.IndexOf('(') + 1;
                var closeIndex = e.Message.Embeds[0].Description.IndexOf(')');
                var uqName = e.Message.Embeds[0].Description.Substring(openIndex, closeIndex - openIndex);

                var registerButton = new DiscordButtonComponent(ButtonStyle.Success, $"register_button_{uqName}", "Party up");
                var msg = new DiscordMessageBuilder()
                    .WithEmbed(GetPartyingEmbed(uqName + " is happening at " + uqTime + "!", e.Message.Embeds[0].Image.Url.ToString()))
                    .AddComponents(new DiscordComponent[] { registerButton });
                var message = await partyChannel.SendMessageAsync(msg).ConfigureAwait(false);

                // Will wait for 40ish minutes
                var _ = await message.CollectReactionsAsync();
                await message.DeleteAsync("The uq is over").ConfigureAwait(false);
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles adding/removing members in the party embed.
        /// </summary>
        private static Task RegisterOrDeregisterPartyMember(DiscordClient client, ComponentInteractionCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                var skipEmptyUserCheck = false;
                var uqName = string.Empty;
                if (e.Message.Embeds.FirstOrDefault() is not DiscordEmbed)
                {
                    return;
                }

                if (string.IsNullOrEmpty(e.Message.Embeds[0].Description))
                {
                    skipEmptyUserCheck = true;
                }

                uqName = e.Message.Embeds[0].Title.Substring(e.Message.Embeds[0].Title.IndexOf(' '));
                var registerButton = new DiscordButtonComponent(ButtonStyle.Success, $"register_button_{uqName}", "Party up");
                var leaveButton = new DiscordButtonComponent(ButtonStyle.Danger, $"leave_button_{uqName}", "Leave");

                if (e.Id.Contains("register"))
                {
                    // If the user is already participating, update the embed with the same content.
                    if (!skipEmptyUserCheck && e.Message.Embeds[0].Description.Contains(e.User.Username))
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                            new DiscordInteractionResponseBuilder()
                                .AddEmbed(e.Message.Embeds[0])
                                .AddComponents(new DiscordComponent[] { registerButton, leaveButton }));
                        return;
                    }

                    var embedDescription = string.Empty;
                    var thumbnailUrl = string.Empty;
                    if (!skipEmptyUserCheck)
                    {
                        var indexEmptyUser = e.Message.Embeds[0].Description.IndexOf(". \n") + 2;
                        if (indexEmptyUser == -1)
                        {
                            // Update embed and remove join button
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                new DiscordInteractionResponseBuilder()
                                    .AddEmbed(e.Message.Embeds[0])
                                    .AddComponents(leaveButton));

                            // Add new embed because the first mpa is full
                            var msg = new DiscordMessageBuilder()
                                .WithEmbed(GetPartyingEmbed(e.Message.Embeds[0].Title, e.Message.Embeds[0].Thumbnail.Url.ToString()))
                                .AddComponents(new DiscordComponent[] { registerButton, leaveButton });
                            var message = await e.Channel.SendMessageAsync(msg).ConfigureAwait(false);

                            // Will wait for 40ish minutes
                            var _ = await message.CollectReactionsAsync();
                            await message.DeleteAsync("The uq is over").ConfigureAwait(false);
                            return;
                        }

                        embedDescription = e.Message.Embeds[0].Description.Substring(0, indexEmptyUser) + e.User.Username + e.Message.Embeds[0].Description.Substring(indexEmptyUser);
                        thumbnailUrl = e.Message.Embeds[0].Thumbnail.Url.ToString();
                    }
                    else
                    {
                        embedDescription = $"Group 1:\n1. {e.User.Username}\n2. \n3. \n4. \n\nGroup2:\n5. \n6. \n7. \n8. \n";
                        thumbnailUrl = e.Message.Embeds[0].Image.Url.ToString();
                    }

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .AddEmbed(new DiscordEmbedBuilder()
                                .WithTitle(e.Message.Embeds[0].Title)
                                .WithThumbnail(thumbnailUrl)
                                .WithDescription(embedDescription))
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
        }

        /// <summary>
        /// Welcome embed posting
        /// </summary>
        private static Task PostWelcomeEmbed(DiscordClient client, GuildMemberAddEventArgs e)
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
        }

        /// <summary>
        /// Membership screening logic
        /// => Gives out Homie role if a member accepts the discord in-built rules
        /// </summary>
        private static Task MembershipScreeningRoleGrant(DiscordClient client, GuildMemberUpdateEventArgs e)
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
        }

        /// <summary>
        /// Logic of the kick/leave/ban embed
        /// </summary>
        private static Task PostKickLeaveBanEmbed(DiscordClient client, GuildMemberRemoveEventArgs e)
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
        }

        /// <summary>
        /// A blueprint for the partying embed.
        /// </summary>
        private static DiscordEmbed GetPartyingEmbed(string title, string imageUrl)
        {
            var embed = new DiscordEmbedBuilder();

            if (!string.IsNullOrEmpty(title))
            {
                embed.WithTitle(title);
            }

            if (!string.IsNullOrEmpty(imageUrl))
            {
                embed.WithImageUrl(imageUrl);
            }

            return embed;
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
    }
}

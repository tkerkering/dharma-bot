using Anotar.Serilog;
using Dharma_DSharp.Constants;
using Dharma_DSharp.Data;
using Dharma_DSharp.Extensions;
using Dharma_DSharp.Models;
using Dharma_DSharp.Modules.Dharma;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using static Dharma_DSharp.Constants.DharmaConstants;

namespace Dharma_DSharp
{
    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            // Add serilog console sink
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            AddOrUpdateAllianceMember(148558164416135168, string.Empty, string.Empty, string.Empty).GetAwaiter().GetResult();
            AddOrUpdateAllianceMember(148558164416135165, string.Empty, string.Empty, string.Empty).GetAwaiter().GetResult();
            AddOrUpdateAllianceMember(148558164416135163, string.Empty, string.Empty, string.Empty).GetAwaiter().GetResult();

            GetAllianceMembers(new AppDbContext());

            var discordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = TryGetToken(args),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                LargeThreshold = 300,
                LoggerFactory = new LoggerFactory().AddSerilog()
            });
            CreateCommandExtensions(discordClient);

            MainAsync(discordClient).GetAwaiter().GetResult();
        }

        private static void GetAllianceMembers(AppDbContext database)
        {
            var members = database.Member?.AsNoTracking();
            if (members == null || !members.Any())
            {
                LogTo.Information("No alliance members found");
                return;
            }

            foreach (var member in members)
            {
                LogTo.Information(member.ToString());
            }
        }

        private static async Task AddOrUpdateAllianceMember(ulong userId, string displayName, string userName, string phantasyId)
        {
            using var context = new AppDbContext();
            var newMember = context.Member
                .FirstOrDefault(b => b.DiscordUserId == userId);
            if (newMember is null)
            {
                newMember = new AllianceMember
                {
                    DiscordUserId = userId,
                    DiscordDisplayName = displayName,
                    DiscordUserName = userName,
                    LastActivityUpdate = DateTime.Now,          // TODO: Fix me
                    PhantasyUserId = phantasyId
                };
            }
            newMember.DiscordDisplayName = string.IsNullOrEmpty(displayName) ? newMember.DiscordDisplayName : displayName;
            newMember.DiscordUserName = string.IsNullOrEmpty(userName) ? newMember.DiscordUserName : userName;
            newMember.PhantasyUserId = string.IsNullOrEmpty(phantasyId) ? newMember.PhantasyUserId : phantasyId;

            context.Member.Attach(newMember);
            context.Member.AddOrUpdate(newMember);
            await context.SaveChangesAsync().ConfigureAwait(false);
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
            discordClient.MessageCreated += (s, e) =>
            {
                return Task.CompletedTask;
            };

            // Used for welcome-embed
            discordClient.GuildMemberAdded += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    var welcomeDescription = Strings.WelcomeDescription.Replace("{0}", e.Member.Id.ToString()).Replace("{1}", GuildId.ToString()).Replace("{2}", ChannelIds.StartHere.ToString());
                    await SendEmbedToWelcomeHall(client,
                        welcomeDescription,
                        new DiscordColor(Strings.WelcomeEmbedColor),
                        Strings.WelcomeTitle,
                        Strings.WelcomeThumbnailUrl,
                        Strings.WelcomeFooterImage).ConfigureAwait(false);
                });

                return Task.CompletedTask;
            };

            discordClient.GuildMemberUpdated += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    // Only process member updates for dharma.
                    if (e.Guild.Id != GuildId)
                    {
                        return;
                    }

                    await AddOrUpdateAllianceMember(e.Member.Id, e.Member.DisplayName, e.Member.Username, string.Empty).ConfigureAwait(false);

                    // For membership screening we don't have to process people that have 0 roles
                    if (e.RolesBefore.Count != 0)
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

            var slash = discordClient.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = serviceProvider
            });

            // Register all command classes here
            slash.RegisterCommands<GrantCommands>(DharmaConstants.GuildId);
            slash.RegisterCommands<ListMembersWithRoleCommand>(DharmaConstants.GuildId);
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
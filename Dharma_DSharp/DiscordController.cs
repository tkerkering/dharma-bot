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
using Dharma_DSharp.Handler;

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
                LogTo.Information("New message in ngs alert channel, checking if it's an alert!");

                await PartyingSystemHandler.HandlePhantasyStarFleetAlert(client, e).ConfigureAwait(false);
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
                if (e.Guild.Id != GuildId || e.Channel.Id != ChannelIds.PartyingChannel || e.Message.Embeds.FirstOrDefault() == null)
                {
                    return;
                }

                await PartyingSystemHandler.RegisterOrDeregisterPartyMember(client, e).ConfigureAwait(false);
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

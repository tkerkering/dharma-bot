using Anotar.Serilog;
using Dharma_DSharp.Constants;
using Dharma_DSharp.Modules.Dharma;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

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
        /// <param name="discordClient"></param>
        private static void HookEventListeners(DiscordClient discordClient)
        {
            discordClient.MessageCreated += (s, e) =>
            {
                // TODO: Add leveling system here, needs database first, too bad!
                return Task.CompletedTask;
            };

            discordClient.GuildMemberAdded += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    await SendEmbedToWelcomeHall(client,
                        $"Welcome to Dharma <@{e.Member.Id}>!\n Please head over to[#start-here](https://discord.com/channels/{DharmaConstants.GuildId}/{DharmaConstants.ChannelIds.StartHere}) to get basic access to several parts of our community.",
                        DiscordColor.SpringGreen,
                        "Please give a warm welcome to our newest guest~",
                        "https://i.imgur.com/JQIxLPQ.png",
                        "https://i.imgur.com/uc89PmB.gif").ConfigureAwait(false);
                });

                return Task.CompletedTask;
            };

            discordClient.GuildBanAdded += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    await SendEmbedToWelcomeHall(client,
                        $"Goodbye <@{e.Member.Id}>. We tried.",
                        DiscordColor.DarkRed,
                        string.Empty,
                        string.Empty,
                        "https://c.tenor.com/S-Lbnq4B-KUAAAAM/good-bye-bye-bye.gif").ConfigureAwait(false);
                });

                return Task.CompletedTask;
            };

            discordClient.GuildMemberRemoved += (client, e) =>
            {
                _ = Task.Run(async () =>
                {
                    var auditLog = e.Guild.GetAuditLogsAsync(10).Result;
                    var kickLogs = auditLog.Where(singleLog => singleLog.ActionType == AuditLogActionType.Kick)?.FirstOrDefault(log => ((DiscordAuditLogKickEntry)log).Target.Id == e.Member.Id);
                    var banLogs = auditLog.Where(singleLog => singleLog.ActionType == AuditLogActionType.Ban)?.FirstOrDefault(log => ((DiscordAuditLogBanEntry)log).Target.Id == e.Member.Id);

                    if (banLogs != null)
                    {
                        return;
                    }

                    if (kickLogs != null)
                    {
                        // User kicked
                        await SendEmbedToWelcomeHall(client,
                            $"Welp, we tried <@{e.Member.Id}>.",
                            DiscordColor.Orange).ConfigureAwait(false);
                        return;
                    }

                    // User left
                    await SendEmbedToWelcomeHall(client,
                        $"Welp, we tried <@{e.Member.Username}>.",
                        DiscordColor.Grayple).ConfigureAwait(false);
                });

                return Task.CompletedTask;
            };
        }

        /// <param name="discordClient"></param>
        /// <param name="description"></param>
        /// <param name="title"></param>
        /// <param name="topRightCornerThumbnail">Provide a valid uri.</param>
        /// <param name="footerImage">Provide a valid uri.</param>
        /// <param name="footer"></param>
        /// <returns></returns>
        private async static Task SendEmbedToWelcomeHall(DiscordClient discordClient,
            string description,
            DiscordColor discordColor,
            string title = "",
            string topRightCornerThumbnail = "",
            string footerImage = "")
        {
            var welcomeHall = await discordClient.GetChannelAsync(DharmaConstants.ChannelIds.WelcomeHall);
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
;
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
using Anotar.Serilog;
using Dharma_DSharp.Handler;
using Dharma_DSharp.Modules.Dharma;
using DSharpPlus;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Dharma_DSharp.Constants;

namespace Dharma_DSharp
{
    internal sealed class Program
    {
        private static DiscordController? _discordController;

        private static void Main(string[] args)
        {
            // Add serilog first
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var serviceProvider = new ServiceCollection()
                .AddSingleton<PartyingSystemHandler>()
                .AddSingleton<DiscordController>()
                .BuildServiceProvider();

            var discordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = TryGetToken(args),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                LargeThreshold = 300,
                LoggerFactory = new LoggerFactory().AddSerilog()
            });

            CreateCommandExtensions(discordClient, serviceProvider);
            _discordController = serviceProvider.GetService<DiscordController>();

            MainAsync(discordClient).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(DiscordClient discordClient)
        {
            await _discordController!.TryConnectDiscordBot(discordClient).ConfigureAwait(false);
            _discordController.HookEventListeners(discordClient);

            LogTo.Information("Started successfully~");
            
            // If no slash command is registered, outcomment the registration below, we can overwrite all commands with nothing.
            // This is useful for removing the duplicated commands of the test bot.
            // await discordClient.BulkOverwriteGuildApplicationCommandsAsync(DharmaConstants.GuildId, new List<DiscordApplicationCommand>()).ConfigureAwait(false);

            await Task.Delay(-1).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the <see cref="ServiceProvider"/> and the <see cref="SlashCommandsExtension"/>.
        /// </summary>
        private static void CreateCommandExtensions(DiscordClient discordClient, ServiceProvider provider)
        {
            discordClient.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromMinutes(40)
            });

            var slash = discordClient.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = provider
            });

            // Register all command classes here
            slash.RegisterCommands<GrantCommands>(DharmaConstants.GuildId);
            slash.RegisterCommands<MoveCommands>(DharmaConstants.GuildId);
            slash.RegisterCommands<ListMembersWithRoleCommand>(DharmaConstants.GuildId);
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
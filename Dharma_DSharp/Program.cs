using Anotar.Serilog;
using Dharma_DSharp.Constants;
using Dharma_DSharp.Modules.Dharma;
using DSharpPlus;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using static System.Net.Mime.MediaTypeNames;

namespace Dharma_DSharp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var discordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = TryGetToken(args),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged,
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
                if (e.Message.Content.ToLower().StartsWith("something"))
                {
                    // await e.Message.RespondAsync("something else!");
                }
                return Task.CompletedTask;
            };

            discordClient.GuildMemberAdded += (s, e) =>
            {
                return Task.CompletedTask;
            };
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
        /// <returns></returns>
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
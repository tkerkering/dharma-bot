using Anotar.Serilog;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dharma_DSharp
{
    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            // Add serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var discordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = TryGetToken(args),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                LargeThreshold = 300,
                LoggerFactory = new LoggerFactory().AddSerilog()
            });
            DiscordController.CreateCommandExtensions(discordClient);

            MainAsync(discordClient).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(DiscordClient discordClient)
        {
            await DiscordController.TryConnectDiscordBot(discordClient).ConfigureAwait(false);
            DiscordController.HookEventListeners(discordClient);

            await Task.Delay(-1).ConfigureAwait(false);
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
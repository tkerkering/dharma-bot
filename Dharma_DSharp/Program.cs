﻿using Anotar.Serilog;
using Dharma_DSharp.Data;
using Dharma_DSharp.Extensions;
using Dharma_DSharp.Models;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

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

#if DEBUG
            AddOrUpdateAllianceMember(148558164416135168, string.Empty, string.Empty, string.Empty).GetAwaiter().GetResult();
            AddOrUpdateAllianceMember(148558164416135165, string.Empty, string.Empty, string.Empty).GetAwaiter().GetResult();
            AddOrUpdateAllianceMember(148558164416135163, string.Empty, string.Empty, string.Empty).GetAwaiter().GetResult();

            GetAllianceMembers(new AppDbContext());
#endif

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
                    LastActivityUpdate = DateTime.Now,          // TODO: Fix/Remove me? Idk
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
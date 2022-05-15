using Anotar.Serilog;
using Dharma_DSharp.CheckAttributes;
using Dharma_DSharp.Constants;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace Dharma_DSharp.Modules.Dharma
{
    internal class GrantCommands : ApplicationCommandModule
    {
        public Random Rando { private get; set; }    // Implied public setter.

        [SlashCommand("grant", Strings.GrantCommandDescription)]
        [SlashRequireGuild]
        [SlashRequireOfficerId]
        public async Task GrantCommand(InteractionContext ctx,
            [Option(Strings.GrantCommandUserParameterName, Strings.GrantCommandUserParameterDescription)] DiscordUser user)
        {
            LogTo.Debug($"{ctx.Member.DisplayName} is trying to grant {user.Username}");
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource).ConfigureAwait(false);
            var targetUser = (DiscordMember)user;

            var isHomie = targetUser.Roles.Any(x => x.Id == DharmaConstants.HomieId);
            if (!isHomie)
            {
                LogTo.Debug($"{user.Username} needs to accept the rules first.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Strings.GrantCommandNeedsToAcceptRules.Replace("{0}", targetUser.Username))).ConfigureAwait(false);
                return;
            }

            var isAlreadyArksOperative = targetUser.Roles.Any(x => x.Id == DharmaConstants.ArksOperative);
            if (isAlreadyArksOperative)
            {
                LogTo.Debug($"{user.Username} is already arks operative..");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Strings.GrantCommandAlreadyGrantedResponse.Replace("{0}", targetUser.Username))).ConfigureAwait(false);
                return;
            }

            var arksRole = ctx.Guild.Roles.FirstOrDefault(role => role.Key == DharmaConstants.ArksOperative);
            LogTo.Debug("Trying to grant {user}", targetUser.Username);
            await targetUser.GrantRoleAsync(arksRole.Value, "Granted by " + ctx.Member.Username).ConfigureAwait(false);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Strings.GrantCommandResponse.Replace("{0}", user.Username))).ConfigureAwait(false);
        }

        [ContextMenu(ApplicationCommandType.UserContextMenu, "Grant Arks")]
        [SlashRequireGuild]
        [SlashRequireOfficerId]
        public async Task GrantByUserMenu(ContextMenuContext ctx)
        {
            LogTo.Debug($"{ctx.Member.Username} is trying to grant {ctx.TargetMember.Username}");
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource).ConfigureAwait(false);

            var isHomie = ctx.TargetMember.Roles.Any(x => x.Id == DharmaConstants.HomieId);
            if (!isHomie)
            {
                LogTo.Debug($"{ctx.TargetMember.Username} needs to accept the rules first.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Strings.GrantCommandNeedsToAcceptRules.Replace("{0}", ctx.TargetMember.Username))).ConfigureAwait(false);
                return;
            }

            var isAlreadyArksOperative = ctx.TargetMember.Roles.Any(x => x.Id == DharmaConstants.ArksOperative);
            if (isAlreadyArksOperative)
            {
                LogTo.Debug($"{ctx.TargetMember.Username} is already arks operative..");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Strings.GrantCommandAlreadyGrantedResponse.Replace("{0}", ctx.TargetMember.Username))).ConfigureAwait(false);
                return;
            }

            var arksRole = ctx.Guild.Roles.FirstOrDefault(role => role.Key == DharmaConstants.ArksOperative);
            LogTo.Debug("Trying to grant {user}", ctx.TargetMember.Username);
            await ctx.TargetMember.GrantRoleAsync(arksRole.Value, "Granted by " + ctx.Member.Username).ConfigureAwait(false);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Strings.GrantCommandResponse.Replace("{0}", ctx.TargetMember.Username))).ConfigureAwait(false);
        }
    }
}

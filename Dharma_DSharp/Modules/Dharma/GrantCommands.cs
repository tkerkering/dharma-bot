using Anotar.Serilog;
using Dharma_DSharp.CheckAttributes;
using Dharma_DSharp.Constants;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using static Dharma_DSharp.Constants.DharmaConstants;

namespace Dharma_DSharp.Modules.Dharma
{
    internal class GrantCommands : ApplicationCommandModule
    {
        [SlashCommand("grant", Strings.GrantCommandDescription)]
        [SlashRequireOfficerId]
        public async Task GrantCommand(InteractionContext ctx,
            [Option(Strings.GrantCommandUserParameterName, Strings.GrantCommandUserParameterDescription)] DiscordUser user)
        {
            await GrantUserByAuthor(ctx.Member, (DiscordMember)user, ctx).ConfigureAwait(false);
        }

        [ContextMenu(ApplicationCommandType.UserContextMenu, "Grant Arks")]
        [SlashRequireOfficerId]
        public async Task GrantByUserMenu(ContextMenuContext ctx)
        {
            await GrantUserByAuthor(ctx.Member, ctx.TargetMember, ctx).ConfigureAwait(false);
        }

        private static async Task GrantUserByAuthor(DiscordMember author, DiscordMember target, BaseContext ctx)
        {
            LogTo.Debug($"{author.Username} is trying to grant {target.Username}");
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource).ConfigureAwait(false);

            var isHomie = target.Roles.Any(x => x.Id == RoleIds.HomieId);
            if (!isHomie)
            {
                LogTo.Debug($"{target.Username} needs to accept the rules first.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Strings.GrantCommandNeedsToAcceptRules.Replace("{0}", target.Username))).ConfigureAwait(false);
                return;
            }

            var isAlreadyArksOperative = target.Roles.Any(x => x.Id == RoleIds.ArksOperative);
            if (isAlreadyArksOperative)
            {
                LogTo.Debug($"{target.Username} is already arks operative..");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Strings.GrantCommandAlreadyGrantedResponse.Replace("{0}", target.Username))).ConfigureAwait(false);
                return;
            }

            var arksRole = ctx.Guild.Roles.FirstOrDefault(role => role.Key == RoleIds.ArksOperative);
            LogTo.Debug("Trying to grant {user}", target.Username);
            await target.GrantRoleAsync(arksRole.Value, "Granted by " + author.Username).ConfigureAwait(false);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Strings.GrantCommandResponse.Replace("{0}", target.Username))).ConfigureAwait(false);
        }
    }
}

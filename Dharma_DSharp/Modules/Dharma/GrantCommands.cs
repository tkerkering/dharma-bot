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
            await SetRolesByAuthor(ctx.Member, (DiscordMember)user, ctx, RoleIds.ArksOperative, RoleIds.HomieId).ConfigureAwait(false);
        }

        [ContextMenu(ApplicationCommandType.UserContextMenu, "Grant Arks")]
        [SlashRequireOfficerId]
        public async Task GrantByUserMenu(ContextMenuContext ctx)
        {
            await SetRolesByAuthor(ctx.Member, ctx.TargetMember, ctx, RoleIds.ArksOperative, RoleIds.HomieId).ConfigureAwait(false);
        }

        [SlashCommand("set_active_member", "Sets an arks veteran to arks operative.")]
        [SlashRequireOfficerId]
        public async Task SetActiveMember(InteractionContext ctx,
            [Option("arks_veteran", "The target member.")] DiscordUser user)
        {
            await SetRolesByAuthor(ctx.Member, (DiscordMember)user, ctx, RoleIds.ArksOperative, RoleIds.VeteranArksOperative, false, RoleIds.VeteranArksOperative).ConfigureAwait(false);
        }

        [SlashCommand("set_inactive_member", "Sets an arks operative to arks veteran.")]
        [SlashRequireOfficerId]
        public async Task SetInactiveMember(InteractionContext ctx,
            [Option("arks_operative", "The target member.")] DiscordUser user)
        {
            await SetRolesByAuthor(ctx.Member, (DiscordMember)user, ctx, RoleIds.VeteranArksOperative, RoleIds.ArksOperative, false, RoleIds.ArksOperative).ConfigureAwait(false);
        }

        private static async Task SetRolesByAuthor(DiscordMember author, DiscordMember target, BaseContext ctx, ulong roleToGive, ulong neededRole, bool isGrantCommand = true, ulong roleToRemove = 0)
        {
            LogTo.Debug($"{author.Username} is trying to grant {target.Username}");
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource).ConfigureAwait(false);

            var searchRole = ctx.Guild.Roles.FirstOrDefault(role => role.Key == neededRole);
            var targetRole = ctx.Guild.Roles.FirstOrDefault(role => role.Key == roleToGive);
            var commandResponse = string.Empty;

            var hasNeededRole = target.Roles.Any(x => x.Id == neededRole);
            if (!hasNeededRole)
            {
                LogTo.Debug($"{target.Username} needs to accept the rules first.");
                commandResponse = isGrantCommand
                    ? Strings.GrantCommandNeedsToAcceptRules.Replace("{0}", target.Username)
                    : $"{target.Username} needs {searchRole.Value.Name} before being able to get {targetRole.Value.Name}.";
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(commandResponse)).ConfigureAwait(false);
                return;
            }

            var targetHasRoleAlready = target.Roles.Any(x => x.Id == roleToGive);
            if (targetHasRoleAlready)
            {
                var response = isGrantCommand
                    ? Strings.GrantCommandAlreadyGrantedResponse.Replace("{0}", target.Username)
                    : $"{target.Username} is already {targetRole.Value.Name}.";
                LogTo.Debug(response);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response)).ConfigureAwait(false);
                return;
            }

            LogTo.Debug($"Trying to grant {target.Username}");
            await target.GrantRoleAsync(targetRole.Value, "Granted by " + author.Username).ConfigureAwait(false);

            commandResponse = isGrantCommand
                ? Strings.GrantCommandResponse.Replace("{0}", target.Username)
                : string.Empty;

            if (roleToRemove != 0)
            {
                var removeRole = ctx.Guild.Roles.FirstOrDefault(role => role.Key == roleToRemove);
                await target.RevokeRoleAsync(removeRole.Value, $"Replaced with {targetRole.Value.Name}");
                commandResponse = $"{target.Username} was given {targetRole.Value.Name}, which replaced {removeRole.Value.Name}!";
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(commandResponse)).ConfigureAwait(false);
        }
    }
}

using DSharpPlus.SlashCommands;
using static Dharma_DSharp.Constants.DharmaConstants;

namespace Dharma_DSharp.CheckAttributes
{
    /// <summary>
    /// Checks if the command is triggered from a guild and if the <see cref="DSharpPlus.Entities.DiscordMember"/> 
    /// </summary>
    internal class SlashRequireOfficerIdAttribute : SlashCheckBaseAttribute
    {
        // may also be asnyc
        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            if (ctx.Guild == null)
            {
                return Task.FromResult(false);
            }

            var member = ctx.Member;
            if (member == null)
            {
                return Task.FromResult(false);
            }

            var isOfficer = member.Roles.Select(x => x.Id).Intersect(RoleIds.AllOfficers).Any();
            if (!isOfficer)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}

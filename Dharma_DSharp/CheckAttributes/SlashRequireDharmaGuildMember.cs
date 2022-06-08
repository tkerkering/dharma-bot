using DSharpPlus.SlashCommands;
using static Dharma_DSharp.Constants.DharmaConstants;

namespace Dharma_DSharp.CheckAttributes
{
    /// <summary>
    /// Checks if the command is triggered from a guild and if the <see cref="DSharpPlus.Entities.DiscordMember"/> 
    /// </summary>
    internal class SlashRequireDharmaGuildMemberAttribute : SlashCheckBaseAttribute
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

            var memberEligible = member.Roles.Select(x => x.Id).Intersect(RoleIds.AllGuildMembers).Any();
            if (!memberEligible)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}

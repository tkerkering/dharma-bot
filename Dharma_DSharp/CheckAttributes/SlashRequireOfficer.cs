using Dharma_DSharp.Constants;
using DSharpPlus.SlashCommands;

namespace Dharma_DSharp.CheckAttributes
{
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

            var isOfficer = member.Roles.Select(x => x.Id).Intersect(DharmaConstants.AllOfficers).Any();
            if (!isOfficer)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}

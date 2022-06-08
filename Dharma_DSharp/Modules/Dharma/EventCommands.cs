using Dharma_DSharp.CheckAttributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dharma_DSharp.Constants.DharmaConstants;

namespace Dharma_DSharp.Modules.Dharma
{
    internal class EventCommands : ApplicationCommandModule
    {

        [SlashCommand("create_event", "Creates an event announcement in the current channel.")]
        [SlashRequireDharmaGuildMember]
        public async Task CreateEventCommand(InteractionContext ctx,
            [Option("tbd", "tbd")] DiscordUser user)
        {
            await Task.Delay(1).ConfigureAwait(false);
        }

        [SlashCommand("remove_event", "Removes an event announcement in the current channel with the specified id.")]
        [SlashRequireDharmaGuildMember]
        public async Task RemoveEventCommand(InteractionContext ctx,
            [Option("tbd", "tbd")] DiscordUser user)
        {
            await Task.Delay(1).ConfigureAwait(false);
        }
    }
}

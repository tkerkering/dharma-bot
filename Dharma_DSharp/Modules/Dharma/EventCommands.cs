using Dharma_DSharp.CheckAttributes;
using DSharpPlus;
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
            [Option("event_title", "The title of your event")] string eventTitle,
            [Option("event_description", "Your event description. Pass in roles via <@&RoleId>.")] string eventDescription,
            [Option("event_date", "Input a valid discord timestamp with your format. https://r.3v.fi/discord-timestamps/")] string dateWhenEventOccurs,
            [Option("event_length", "String -> Anything is allowed")] string eventLength,
            [Option("max_attendees", "The max amount of attendees for the event.")] long maxEventAttendees,
            [Option("picture_url", "The picture for the event.")] string eventPicture = "",
            [Option("attendees_per_group", "Members per group, make sure that max amount is dividable by attendees per group.")] long attendeesPerGroup = 0,
            [Option("level_requirement", "If it's a game related event, set the level requirements here.")] long? levelRequirement = 0,
            [Option("bp_requirement", "If it's a pso related event, set the bp requirement here.")] long? bpRequirement = 0,
            [Option("weapon_potency_requirement", "If it's a pso related event, set the weapon potency requirement here.")] long? weaponPotencyRequirement = 0
            )
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource).ConfigureAwait(false);
            var header = "__**Fixed description:**__\n";
            var firstLineOfDescription = header + $"{ctx.User.Mention} will host an event at {dateWhenEventOccurs}!\n";
            var secondLineOfDescription = $"{firstLineOfDescription}The event has a length of {eventLength}!\n";
            var thirdLineOfDescription = $"{secondLineOfDescription}The event has a maximum amount of |{maxEventAttendees}| attendees.\n";
            var fourthLineOfDescription = thirdLineOfDescription + (attendeesPerGroup != 0 ? $"The event is split into |{maxEventAttendees / attendeesPerGroup}| groups, each group has |{attendeesPerGroup}| members.\n" : "");
            var fifthLineOfDescription = fourthLineOfDescription + (levelRequirement != 0 ? $"The event has a level requirement of {levelRequirement}\n" : "");
            var sixthLineOfDescription = fifthLineOfDescription + (bpRequirement != 0 ? $"The event has a bp-requirement of {bpRequirement}\n" : "");
            var seventhLineOfDescription = sixthLineOfDescription + (weaponPotencyRequirement != 0 ? $"The event has a weapon-potency-requirement of {weaponPotencyRequirement}\n\n" : "\n");
            var eightLine = seventhLineOfDescription + "__**Event description:**__\n" + eventDescription;
            var appendedMemberPart = "";
            if (attendeesPerGroup != 0)
            {
                appendedMemberPart = eightLine + $"\n\n__**Attendees:**__\n" + "Group 1:\n1. " + ctx.User.Username;
            }
            else
            {
                appendedMemberPart = eightLine + $"\n\n__**Attendees:**__\n" + "1. " + ctx.User.Username;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle(eventTitle)
                .WithDescription(appendedMemberPart)
                .WithFooter("Delivered by Dharma Bot")
                .WithThumbnail("https://cdn.discordapp.com/attachments/883571434419552276/883571609292640286/0CGU3b7.png");
            if (!string.IsNullOrEmpty(eventPicture))
            {
                embed.WithImageUrl(eventPicture);
            }

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed)
                .AddComponents(GetRegisterButton());
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Verified integrity of data. Posting event now!")).ConfigureAwait(false);
            await ctx.Channel.SendMessageAsync(message);
        }

        [SlashCommand("remove_event", "Removes an event announcement in the current channel with the specified id.")]
        [SlashRequireDharmaGuildMember]
        public async Task RemoveEventCommand(InteractionContext ctx,
            [Option("message_id", "The message id of the event to remove.")] string messageId)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource).ConfigureAwait(false);
            try
            {
                var channel = ctx.Channel;
                var idAsNumber = Convert.ToUInt64(messageId);
                var message = await ctx.Channel.GetMessageAsync(idAsNumber).ConfigureAwait(false);
                if (message == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Couldn't find the message with the given id.")).ConfigureAwait(false);
                    return;
                }

                if (message.Embeds.Count > 0)
                {
                    var isAuthorized = message.Embeds[0].Description.Contains(ctx.User.Id.ToString());
                    if (isAuthorized)
                    {
                        await message.DeleteAsync("Host wanted to delete his event").ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is FormatException or OverflowException)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Couldn't convert the given message id.")).ConfigureAwait(false);
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Couldn't authorize member as the author.")).ConfigureAwait(false);
                }
            }
        }

        private DiscordButtonComponent GetRegisterButton() => new(ButtonStyle.Success, $"e_register_button_{DateTime.Now.ToString("yyyy_MM_dd_hh_ss")}", "Join");

        private DiscordButtonComponent GetLeaveButton() => new(ButtonStyle.Danger, $"e_leave_button_{DateTime.Now.ToString("yyyy_MM_dd_hh_ss")}", "Leave");
    }
}

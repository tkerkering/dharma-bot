using DSharpPlus.EventArgs;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Anotar.Serilog;

namespace Dharma_DSharp.Handler
{
    public class RegistrationDeregistrationHandler
    {
        public async Task RegisterOrDeregisterEventAttendee(DiscordClient _, ComponentInteractionCreateEventArgs e)
        {
            if (e.Id.Contains("e_register"))
            {
                await RegisterUser(e).ConfigureAwait(false);
            }
            else if (e.Id.Contains("e_leave"))
            {
                await DeregisterUser(e).ConfigureAwait(false);
            }
        }

        private async Task RegisterUser(ComponentInteractionCreateEventArgs e)
        {
            var registerButton = GetRegisterButton();
            var leaveButton = GetLeaveButton();

            // If the user is already participating, update the embed with the same content.
            if (e.Message.Embeds[0].Description.Contains(e.User.Username))
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(e.Message.Embeds[0])
                        .AddComponents(new DiscordComponent[] { registerButton, leaveButton }));
                return;
            }

            var cuttedByDivisor = e.Message.Embeds[0].Description.Split('|');
            var cuttedByNewLine = e.Message.Embeds[0].Description.Split("\n");

            var memberLimit = Convert.ToInt32(cuttedByDivisor[1]);
            var hasMembersPerGroup = cuttedByDivisor.Length > 3;
            var currentNumberOfAttendees = 0;
            try
            {
                currentNumberOfAttendees = Convert.ToInt32(cuttedByNewLine.Last().Remove(cuttedByNewLine.Last().IndexOf('.')));
            }
            catch
            {
                LogTo.Error("Coudln't parse attendees, maybe there are none");
            }

            if (currentNumberOfAttendees == memberLimit)
            {
                await e.Channel.SendMessageAsync("Sadly, the event is already full. Maybe check in later and see if that changed.").ConfigureAwait(false);
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(e.Message.Embeds[0])
                        .AddComponents(new DiscordComponent[] { leaveButton }));
                return;
            }

            var newDescription = e.Message.Embeds[0].Description;
            if (hasMembersPerGroup)
            {
                var membersPerGroup = Convert.ToInt32(cuttedByDivisor[5]);
                var stillPlaceInGroup = currentNumberOfAttendees % membersPerGroup != 0;
                if (stillPlaceInGroup)
                {
                    newDescription += $"\n{currentNumberOfAttendees + 1}. {e.User.Username}";
                }
                else
                {
                    newDescription += $"\n\nGroup {(currentNumberOfAttendees / membersPerGroup) + 1}:\n{currentNumberOfAttendees + 1}. {e.User.Username}";
                }
            }
            else
            {
                newDescription += $"\n{currentNumberOfAttendees + 1}. {e.User.Username}";
            }

            var embed = new DiscordEmbedBuilder(e.Message.Embeds[0])
                .WithDescription(newDescription);

            if (currentNumberOfAttendees + 1 != memberLimit)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed)
                        .AddComponents(new DiscordComponent[] { registerButton, leaveButton }));
            }
            else
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed)
                        .AddComponents(new DiscordComponent[] { leaveButton }));
            }
        }

        private async Task DeregisterUser(ComponentInteractionCreateEventArgs e)
        {
            var registerButton = GetRegisterButton();
            var leaveButton = GetLeaveButton();
            var originalEmbed = e.Message.Embeds[0];

            var cuttedByDivisor = originalEmbed.Description.Split('|');
            var cuttedByNewLine = originalEmbed.Description.Split("\n");

            var memberLimit = Convert.ToInt32(cuttedByDivisor[1]);
            var hasMembersPerGroup = cuttedByDivisor.Length > 3;
            var currentNumberOfAttendees = 0;
            try
            {
                currentNumberOfAttendees = Convert.ToInt32(cuttedByNewLine.Last().Remove(cuttedByNewLine.Last().IndexOf('.')));
            }
            catch
            {
                LogTo.Error("Coudln't parse attendees, maybe there are none");
            }

            var embed = new DiscordEmbedBuilder(originalEmbed);
            var splitByHeaders = originalEmbed.Description.Split("__**Attendees:**");
            var attendeeList = splitByHeaders.Last();
            var indexOfUser = attendeeList.IndexOf(e.User.Username);
            if (indexOfUser == -1 || indexOfUser == 0)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(originalEmbed)
                        .AddComponents(new DiscordComponent[] { registerButton, leaveButton }));
                return;
            }

            var individualAttendee = attendeeList.Split("\n").ToList();
            var realAttendee = individualAttendee.Where(s => s.Contains('.')).ToList();
            for (var i = 0; i < realAttendee.Count; i++)
            {
                realAttendee[i] = realAttendee[i].Remove(0, realAttendee[i].IndexOf('.') + 2);
            }

            var newAttendees = realAttendee.Where(entry => !entry.Equals(e.User.Username)).ToList();
            var newDescription = splitByHeaders[0] + "__**Attendees:**__\n";
            if (newAttendees.Count == 0)
            {
                embed.WithDescription(newDescription);
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed)
                        .AddComponents(new DiscordComponent[] { registerButton }));
                return;
            }

            if (hasMembersPerGroup)
            {
                var membersPerGroup = Convert.ToInt32(cuttedByDivisor[3]);
                newDescription += "\nGroup 1:";
                for (var i = 0; i < newAttendees.Count; i++)
                {
                    var stillPlaceInGroup = i == 0 ? true : i % membersPerGroup != 0;
                    if (stillPlaceInGroup)
                    {
                        newDescription += $"\n{i + 1}. {newAttendees[i]}";
                    }
                    else
                    {
                        newDescription += $"\n\nGroup {(i / membersPerGroup) + 1}:\n{i + 1}. {newAttendees[i]}";
                    }
                }
            }
            else
            {
                for (var i = 0; i < newAttendees.Count; i++)
                {
                    newDescription += $"{i + 1}. {newAttendees[i]}\n";
                }
            }

            embed.WithDescription(newDescription);

            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed)
                    .AddComponents(new DiscordComponent[] { registerButton, leaveButton }));
        }

        private DiscordButtonComponent GetRegisterButton() => new(ButtonStyle.Success, $"e_register_button_{DateTime.Now.ToString("yyyy_MM_dd_hh_ss")}", "Join");

        private DiscordButtonComponent GetLeaveButton() => new(ButtonStyle.Danger, $"e_leave_button_{DateTime.Now.ToString("yyyy_MM_dd_hh_ss")}", "Leave");
    }
}

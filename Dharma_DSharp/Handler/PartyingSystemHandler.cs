using DSharpPlus.EventArgs;
using DSharpPlus;
using Dharma_DSharp.Extensions;
using DSharpPlus.Entities;
using static Dharma_DSharp.Constants.DharmaConstants;
using Anotar.Serilog;
using DSharpPlus.Interactivity.Extensions;

namespace Dharma_DSharp.Handler
{
    public class PartyingSystemHandler
    {
        public async Task HandlePhantasyStarFleetAlert(DiscordClient client, MessageCreateEventArgs e)
        {
            var searchString = "is happening in ";
            var indexOfHappening = e.Message.Embeds[0].Description?.IndexOf(searchString) ?? -1;
            if (indexOfHappening == -1 || indexOfHappening == 0)
            {
                LogTo.Information("Not a uq/concert/event");
                return;
            }

            var partyChannel = await client.GetChannelAsync(ChannelIds.PartyingChannel).ConfigureAwait(false);
            if (partyChannel == null)
            {
                LogTo.Information("Party channel not found, please check configuration!");
                return;
            }

            var unixTimeStamp = DateTimeOffset.UtcNow.RoundUpToNearest30().ToUnixTimeSeconds();
            var uqTime = $"<t:{unixTimeStamp}:f>";

            var openIndex = e.Message.Embeds[0].Description.IndexOf('(') + 1;
            var closeIndex = e.Message.Embeds[0].Description.IndexOf(')');
            var uqName = e.Message.Embeds[0].Description.Substring(openIndex, closeIndex - openIndex);
            var isConcert = e.Message.Embeds[0].Description.Contains("concert");
            var (RegisterButton, _) = GetRegisterAndLeaveButton(uqName, isConcert);

            var msg = new DiscordMessageBuilder()
                .WithEmbed(GetPartyingEmbed(uqName + " is happening at " + uqTime + "!", string.Empty, e.Message.Embeds[0].Image.Url.ToString(), string.Empty))
                .AddComponents(new DiscordComponent[] { RegisterButton });
            var message = await partyChannel.SendMessageAsync(msg).ConfigureAwait(false);

            // Will wait for 40ish minutes
            var _ = await message.CollectReactionsAsync();
            await message.DeleteAsync("The uq is over").ConfigureAwait(false);
        }

        public async Task RegisterOrDeregisterPartyMember(DiscordClient _, ComponentInteractionCreateEventArgs e)
        {
            var uqName = e.Message.Embeds[0].Title.Substring(e.Message.Embeds[0].Title.IndexOf(' '));
            var isConcert = e.Message.Embeds[0].Title.Contains("concert");

            if (e.Id.Contains("register"))
            {
                await RegisterUser(e, uqName, isConcert).ConfigureAwait(false);
            }
            else if (e.Id.Contains("leave"))
            {
                await DeregisterUser(e, uqName, isConcert).ConfigureAwait(false);
            }
        }

        private async Task RegisterUser(ComponentInteractionCreateEventArgs e, string uqName, bool isConcert)
        {
            var skipEmptyUserCheck = false;
            if (string.IsNullOrEmpty(e.Message.Embeds[0].Description))
            {
                skipEmptyUserCheck = true;
            }

            var (RegisterButton, LeaveButton) = GetRegisterAndLeaveButton(uqName, isConcert);

            // If the user is already participating, update the embed with the same content.
            if (!skipEmptyUserCheck && e.Message.Embeds[0].Description.Contains(e.User.Username))
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(e.Message.Embeds[0])
                        .AddComponents(new DiscordComponent[] { RegisterButton, LeaveButton }));
                return;
            }

            string? embedDescription;
            string? thumbnailUrl;
            if (!skipEmptyUserCheck)
            {
                var indexEmptyUser = e.Message.Embeds[0].Description.IndexOf(". \n") + 2;
                if (indexEmptyUser == -1)
                {
                    // Update embed and remove join button
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .AddEmbed(e.Message.Embeds[0])
                            .AddComponents(LeaveButton));

                    // Add new embed because the first mpa is full
                    var msg = new DiscordMessageBuilder()
                        .WithEmbed(GetPartyingEmbed(e.Message.Embeds[0].Title, string.Empty, e.Message.Embeds[0].Thumbnail.Url.ToString(), string.Empty))
                        .AddComponents(new DiscordComponent[] { RegisterButton, LeaveButton });
                    var message = await e.Channel.SendMessageAsync(msg).ConfigureAwait(false);

                    // Will wait for 40ish minutes
                    _ = await message.CollectReactionsAsync();
                    await message.DeleteAsync("The uq is over").ConfigureAwait(false);
                    return;
                }

                embedDescription = e.Message.Embeds[0].Description.Substring(0, indexEmptyUser) + e.User.Username + e.Message.Embeds[0].Description.Substring(indexEmptyUser);
                thumbnailUrl = e.Message.Embeds[0].Thumbnail.Url.ToString();
            }
            else
            {
                embedDescription = $"Group 1:\n1. {e.User.Username}\n2. \n3. \n4. \n\nGroup2:\n5. \n6. \n7. \n8. \n";
                thumbnailUrl = e.Message.Embeds[0].Image.Url.ToString();
            }

            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(GetPartyingEmbed(e.Message.Embeds[0].Title, embedDescription, string.Empty, thumbnailUrl))
                    .AddComponents(new DiscordComponent[] { RegisterButton, LeaveButton }));
        }

        private async Task DeregisterUser(ComponentInteractionCreateEventArgs e, string uqName, bool isConcert)
        {
            var (RegisterButton, LeaveButton) = GetRegisterAndLeaveButton(uqName, isConcert);
            var indexOfUser = e.Message.Embeds[0].Description.IndexOf(e.User.Username);
            if (indexOfUser == -1 || indexOfUser == 0)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(e.Message.Embeds[0])
                        .AddComponents(new DiscordComponent[] { RegisterButton, LeaveButton }));
                return;
            }
            var description = e.Message.Embeds[0].Description;
            var withRemovedUser = description.Substring(0, indexOfUser) + description.Substring(indexOfUser + e.User.Username.Length);

            if (withRemovedUser.Split(". \n").Length == 8)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(GetPartyingEmbed(e.Message.Embeds[0].Title, string.Empty, e.Message.Embeds[0].Thumbnail.Url.ToString(), string.Empty))
                    .AddComponents(new DiscordComponent[] { RegisterButton }));
                return;
            }

            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(GetPartyingEmbed(e.Message.Embeds[0].Title, withRemovedUser, string.Empty, e.Message.Embeds[0].Thumbnail.Url.ToString()))
                    .AddComponents(new DiscordComponent[] { RegisterButton, LeaveButton }));
        }

        private (DiscordButtonComponent RegisterButton, DiscordButtonComponent LeaveButton) GetRegisterAndLeaveButton(string uqName, bool isConcert)
        {
            var registerButton = new DiscordButtonComponent(ButtonStyle.Success, $"register_button_{uqName}", isConcert ? "Join" : "Party up");
            var leaveButton = new DiscordButtonComponent(ButtonStyle.Danger, $"leave_button_{uqName}", "Leave");

            return (registerButton, leaveButton);
        }

        /// <summary>
        /// A blueprint for the partying embed.
        /// </summary>
        private DiscordEmbed GetPartyingEmbed(string title, string description, string imageUrl, string thumbnailUrl)
        {
            var embed = new DiscordEmbedBuilder();

            if (!string.IsNullOrEmpty(title))
            {
                embed.WithTitle(title);
            }

            if (!string.IsNullOrEmpty(imageUrl))
            {
                embed.WithImageUrl(imageUrl);
            }

            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                embed.WithThumbnail(thumbnailUrl);
            }

            if (!string.IsNullOrEmpty(description))
            {
                embed.WithDescription(description);
            }

            return embed;
        }
    }
}

using Anotar.Serilog;
using Dharma_DSharp.CheckAttributes;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using System.Xml.Linq;

namespace Dharma_DSharp.Modules.Dharma
{
    internal class MoveCommands : ApplicationCommandModule
    {

        [SlashCommand("batchMove", "Moves the last x-messages to the given channel.")]
        [SlashRequireOfficerId]
        public async Task MoveSeveralMessages(InteractionContext ctx,
            [Option("destination", "Channel to send messages to")] DiscordChannel destination,
            [Option("move_amount", "Amount of messages to move")] long amount)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource).ConfigureAwait(false);
            try
            {
                var messages = await ctx.Channel.GetMessagesAsync(Convert.ToInt32(amount)).ConfigureAwait(false);
                await MoveMessageToOtherChannel(ctx.Member, new List<DiscordMessage>(messages), destination, ctx).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (e is NotFoundException ex)
                {
                    LogTo.Error(ex, "Not found exception");
                }
                else
                {
                    LogTo.Error(e, "Couldn't grab messages that are to be moved");
                }
            }
        }

        private static async Task MoveMessageToOtherChannel(DiscordMember moderator, List<DiscordMessage> messages, DiscordChannel destination, BaseContext ctx)
        {
            LogTo.Information($"Trying to move {messages} to {destination}");
            var messageString = messages.Count > 1 ? "messages" : "message";
            var toDelete = messages.Where(msg => msg.MessageType != MessageType.Default).ToList();
            var filteredMessages = messages.Where(msg => msg.MessageType == MessageType.Default).ToList();

            foreach (var deleteMsg in toDelete)
            {
                await deleteMsg.DeleteAsync().ConfigureAwait(false);
            }

            for (var i = 0; i < filteredMessages.Count; i += 10)
            {
                var amountOfMessagesToMove = (filteredMessages.Count - i) % 10;
                var filledMoveMessage = new DiscordMessageBuilder()
                    .WithContent($"{moderator.Username} moved {amountOfMessagesToMove} {messageString} here!");
                for (var j = 0; j < filteredMessages.Count; j++)
                {
                    var embedToAdd = new DiscordEmbedBuilder()
                        .WithAuthor(filteredMessages[j].Author.Username, string.Empty, filteredMessages[j].Author.AvatarUrl)
                        .WithDescription(filteredMessages[j].Content);
                    if (filteredMessages[j].Attachments.Count != 0)
                    {
                        embedToAdd.WithImageUrl(filteredMessages[j].Attachments[0].Url);
                    }
                    filledMoveMessage.AddEmbed(embedToAdd);
                    await filteredMessages[j].DeleteAsync().ConfigureAwait(false);
                }

                await destination.SendMessageAsync(filledMoveMessage).ConfigureAwait(false);
            }

            var replyContent = $"Moved {messages.Count} {messageString} to <#{destination.Id}>";
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(replyContent)).ConfigureAwait(false);
            LogTo.Information($"Send {messages} to {destination}!");
        }
    }
}

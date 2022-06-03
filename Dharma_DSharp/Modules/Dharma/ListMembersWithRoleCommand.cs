using Anotar.Serilog;
using Dharma_DSharp.CheckAttributes;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Dharma_DSharp.Modules.Dharma
{
    internal class ListMembersWithRoleCommand : ApplicationCommandModule
    {
        [SlashCommand("list", "Lists all members with the given role")]
        [SlashRequireOfficerId]
        public async Task ListMemberCommand(InteractionContext ctx,
            [Option("role", "Role to list members of")] DiscordRole role)
        {
            LogTo.Debug($"{ctx.Member.Username} will list all members with the following role => {role.Name}");
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource).ConfigureAwait(false);

            var allMembers = await ctx.Guild.GetAllMembersAsync().ConfigureAwait(false);
            var response = "";
            foreach (var member in allMembers)
            {
                if (member.Roles.Any(r => r.Id.Equals(role.Id)))
                {
                    response += member.DisplayName + ";" + member.Username + ";" + member.Id + "\n";
                }
            }
            if (response.Length >= 4000)
            {
                var fileName = Path.GetTempPath() + "userList.txt";
                using (var streamWriter = new StreamWriter(fileName))
                {
                    streamWriter.Write(response);
                }
                using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent("Too many users to list proberly").WithFile(fileName, fileStream));
                }
                try
                {
                    File.Delete(fileName);
                }
                catch
                {
                    LogTo.Error("Couldn't delete temporary file");
                }
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
        }
    }
}

#region

using System.Threading.Tasks;
using Discord;
using Discord.Commands;

#endregion

namespace RadBot.Modules
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Name("Debug")]
    [Group("debug")]
    public class DebugModule : ModuleBase<SocketCommandContext>
    {
        [Command("echo")]
        [Summary("Echoes a message.")]
        public async Task SayAsync([Remainder] [Summary("The text to echo")]
            string echo)
        {
            await ReplyAsync(echo);
        }

        [Command("uptime")]
        [Summary("Prints up time in ms.")]
        public async Task UptimeAsync()
        {
            await ReplyAsync("Uptime: " + Helper.Uptime + " ms");
        }
    }
}
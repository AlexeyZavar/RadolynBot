#region

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

#endregion

namespace RadBot.Modules
{
    [Name("Debug")]
    [Group("debug")]
    public class DebugModule : ModuleBase<SocketCommandContext>
    {
        [Command("echo")]
        [Alias("say")]
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
            await ReplyAsync("UpTime: " + Helper.UpTime + " ms");
        }

        [Command("exception")]
        [Alias("exc")]
        [Summary("Throws exception.")]
        public Task ExceptionAsync()
        {
            throw new ApplicationException("Test exception");
        }
    }
}
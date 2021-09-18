#region

using System;
using System.Threading.Tasks;
using Discord.Commands;

#endregion

namespace RadBot.Modules
{
    [RequireOwner]
    [Name("Debug")]
    [Group("debug")]
    public sealed class DebugModule : ModuleBase<SocketCommandContext>
    {
        [Command("echo")]
        [Alias("say")]
        [Summary("Echoes a message.")]
        public async Task Say([Remainder] [Summary("The text to echo")] string echo)
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(echo);
        }

        [Command("uptime")]
        [Summary("Prints up time in ms.")]
        public async Task Uptime()
        {
            await ReplyAsync("My up time is " + Helper.UpTime + " seconds");
        }

        [Command("exception")]
        [Alias("exc")]
        [Summary("Throws exception.")]
        public Task Exception()
        {
            throw new ApplicationException("Test exception");
        }

        [Command("shutdown")]
        [Summary("Shutdowns bot.")]
        public async Task Shutdown()
        {
            await ReplyAsync("Shutting down bot. Current up time: " + Helper.UpTime + " seconds.");

            Environment.Exit(0);
        }
    }
}
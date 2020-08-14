#region

using System;
using System.Threading.Tasks;
using Discord.Commands;

#endregion

namespace RadBot.Modules
{
    [RequireOwner]
    [Name("Develop")]
    [Group("dev")]
    public class InternalModule : ModuleBase<SocketCommandContext>
    {
        [Command("shutdown")]
        [Summary("Shutdowns bot.")]
        public async Task RestartAsync()
        {
            await ReplyAsync("Shutting down bot. Current up time: " + Helper.UpTime + " ms.");

            try
            {
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                await ReplyAsync("Failed to shutdown bot." + Helper.FormatException(e));
            }
        }
    }
}
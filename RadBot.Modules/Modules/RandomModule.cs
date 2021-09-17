#region

using System.Threading.Tasks;
using Discord.Commands;
using RadLibrary.Configuration;

#endregion

namespace RadBot.Modules
{
    [Name("Random")]
    public class RandomModule : ModuleBase<SocketCommandContext>
    {
        [Command("social")]
        [Summary("Prints developer's social links.")]
        public async Task Social()
        {
            var embedBuilder = Helper.GetBuilder();

            embedBuilder.Title = "Social links";

            embedBuilder.AddField("Site:", "https://radolyn.com", true);
            embedBuilder.AddField("Discord Server:", "https://discord.gg/CGFFP2H", true);
            embedBuilder.AddField("GitHub:", "https://github.com/Radolyn", true);
            embedBuilder.AddField("Twitter:", "https://twitter.com/RadolynInc", true);


            await ReplyAsync("", false, embedBuilder.Build());
        }
    }
}

#region

using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RadLibrary.Configuration;

#endregion

namespace RadBot.Modules
{
    [Name("Random")]
    public class RandomModule : ModuleBase<SocketCommandContext>
    {
        private readonly AppConfiguration _config;

        public RandomModule(AppConfiguration config)
        {
            _config = config;
        }

        [Command("social")]
        [Summary("Prints developer's social links.")]
        public async Task RestartAsync()
        {
            var embedBuilder = Helper.GetBuilder();

            embedBuilder.Title = "Social links";

            embedBuilder.AddField("Discord Server:", "https://discord.gg/CGFFP2H", true);
            embedBuilder.AddField("GitHub:", "https://github.com/radolyn", true);
            embedBuilder.AddField("Site:", "https://radolyn.com", true);

            await ReplyAsync("", false, embedBuilder.Build());
        }

        [Command("list")]
        [Summary("Prints current channel users.")]
        public async Task ListAsync()
        {
            var builder = Helper.GetBuilder();

            builder.Title = $"Users list in {Context.Channel.Name}";

            var users = await Context.Channel.GetUsersAsync().FlattenAsync();

            builder.AddField("Users:",
                users.Aggregate("",
                    (current, item) => current + _config["bulletSymbol"] + " " + item.Username + Environment.NewLine));

            await ReplyAsync(embed: builder.Build());
        }
    }
}
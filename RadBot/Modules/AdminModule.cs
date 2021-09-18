#region

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Serilog;

#endregion

namespace RadBot.Modules
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Name("Admin")]
    public sealed class AdminModule : ModuleBase<SocketCommandContext>
    {
        protected override void AfterExecute(CommandInfo command)
        {
            try
            {
                Context.Message.DeleteAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while executing {Method} in {Class}", nameof(AfterExecute), nameof(AdminModule));
            }

            base.AfterExecute(command);
        }

        [Command("ban")]
        public async Task Ban(IGuildUser user, [Remainder] string reason = "no reason")
        {
            var embed = Helper.GetBuilder();

            embed.Title = "❌ Ban ❌";

            try
            {
                await user.BanAsync(reason: reason);

                embed.AddField("Banned user", user.Mention);
                embed.AddField("Reason", reason);
            }
            catch (HttpException e)
            {
                embed.AddField("Failed to ban user", user.Mention);
                embed.AddField("Fail reason", e.Message);
            }

            embed.AddField("Action authorized by", Context.User.Mention);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("kick")]
        public async Task Kick(IGuildUser user, [Remainder] string reason = "no reason")
        {
            var embed = Helper.GetBuilder();

            embed.Title = "❌ Kick ❌";

            try
            {
                await user.KickAsync(reason);

                embed.AddField("Kicked user", user.Mention);
                embed.AddField("Reason", reason);
            }
            catch (HttpException e)
            {
                embed.AddField("Failed to kick user", user.Mention);
                embed.AddField("Fail reason", e.Message);
            }

            embed.AddField("Action authorized by", Context.User.Mention);

            await ReplyAsync(embed: embed.Build());
        }
    }
}
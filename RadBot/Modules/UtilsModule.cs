#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NYoutubeDL;
using RadLibrary.Configuration;
using RadLibrary.Logging;

#endregion

namespace RadBot.Modules
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Name("Utilities")]
    [Group("utils")]
    public class UtilsModule : ModuleBase<SocketCommandContext>
    {
        private readonly AppConfiguration _config;

        public UtilsModule(AppConfiguration config)
        {
            _config = config;
        }

        [Command("dl", RunMode = RunMode.Async)]
        [Summary("Bridge to youtube-dl module.")]
        public async Task YoutubeDownloadAsync([Remainder] [Summary("The url.")] string url)
        {
            var youtubeDl = new YoutubeDL
            {
                YoutubeDlPath = _config["youtube-dl"]
            };

            youtubeDl.Options.VerbositySimulationOptions.GetUrl = true;
            var downloadUrl = "";

            await youtubeDl.GetDownloadInfoAsync(url);

            youtubeDl.StandardOutputEvent += (sender, s) =>
            {
                downloadUrl += s + Environment.NewLine;
                LogManager.GetClassLogger().Trace(s);
            };

            await youtubeDl.PrepareDownloadAsync();
            await youtubeDl.DownloadAsync();

            await ReplyAsync(Context.User.Mention + " " + downloadUrl);
        }

        [Name("Cleaner")]
        [Group("clean")]
        public class Cleaner : ModuleBase<SocketCommandContext>
        {
            [Command("self", RunMode = RunMode.Async)]
            [Summary("Cleans bot's messages in current channel.")]
            public async Task CleanSelfAsync([Summary("The maximum messages to view for deletion.")]
                int max)
            {
                await CleanAsync((ITextChannel) Context.Channel,
                    message => message.Author.Id == Context.Client.CurrentUser.Id ||
                               message.MentionedUserIds.Any(x => x == Context.Client.CurrentUser.Id) ||
                               message.Content.StartsWith('>'), max);
            }

            [Command("user", RunMode = RunMode.Async)]
            [Summary("Cleans user's messages in current channel.")]
            public async Task CleanSelfAsync([Summary("The maximum messages to view for deletion.")]
                int max, [Summary("The user.")] IUser user)
            {
                await CleanAsync((ITextChannel) Context.Channel,
                    message => message.Author.Id == user.Id || message.MentionedUserIds.Any(x => x == user.Id), max);
            }

            [Command("all", RunMode = RunMode.Async)]
            [Summary("Cleans messages in current channel.")]
            public async Task CleanAllAsync([Summary("The maximum messages to view for deletion.")]
                int max)
            {
                await CleanAsync((ITextChannel) Context.Channel, message => true, max);
            }

            private async Task CleanAsync(ITextChannel channel, Predicate<IMessage> predicate, int max)
            {
                await ReplyAsync("Analyzing...");

                var messages = await Context.Channel.GetMessagesAsync(max).FlattenAsync();

                var found = new List<IMessage>();

                foreach (var message in messages)
                {
                    var res = predicate.Invoke(message);
                    var days = (DateTimeOffset.Now - message.Timestamp).TotalDays;
                    if (res && days >= 14)
                    {
                        await Context.Channel.DeleteMessageAsync(message);
                        await Task.Delay(50);
                    }
                    else if (res && days < 14)
                    {
                        found.Add(message);
                    }
                }

                await CleanAsync(found, channel);

                await ReplyAsync("Cleaned!");
            }

            private static async Task CleanAsync(IEnumerable<IMessage> messages, ITextChannel channel)
            {
                await channel.DeleteMessagesAsync(messages);
            }
        }
    }
}
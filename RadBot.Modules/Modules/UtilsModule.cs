#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using NYoutubeDL;
using RadLibrary.Configuration;
using Serilog;

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
        public async Task YoutubeDownload([Remainder] [Summary("The url.")] string url)
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
                Log.Verbose(s);
            };

            await youtubeDl.PrepareDownloadAsync();
            await youtubeDl.DownloadAsync();

            await ReplyAsync(Context.User.Mention + " " + downloadUrl);
        }

        [Name("Audit")]
        [Group("audit")]
        public class AuditModule : ModuleBase<SocketCommandContext>
        {
            [Command("what")]
            [Summary("Prints latest actions from specified user.")]
            public async Task What([Summary("The user.")] IUser user,
                [Summary("The amount of logs.")] int amount = 10, [Summary("The page.")] int page = 1)
            {
                var allActions = await Context.Guild.GetAuditLogsAsync(250 * page, userId: user.Id).FlattenAsync();

                var s = ProcessActions(allActions, amount);

                await ReplyAsync(s);
            }

            [Command("who")]
            [Summary("Prints latest actions with specified user.")]
            public async Task Who([Summary("The user.")] IUser user,
                [Summary("The amount of logs.")] int amount = 10, [Summary("The page.")] int page = 1)
            {
                var allActions = await Context.Guild.GetAuditLogsAsync(250 * page).FlattenAsync();

                var procActions = new List<RestAuditLogEntry>();

                foreach (var action in allActions)
                {
                    switch (action.Action)
                    {
                        case ActionType.Kick:
                            var act1 = action.Data as KickAuditLogData;
                            if (act1.Target.Id != user.Id)
                                continue;
                            break;
                        case ActionType.Ban:
                            var act3 = action.Data as BanAuditLogData;
                            if (act3.Target.Id != user.Id)
                                continue;
                            break;
                        case ActionType.Unban:
                            var act4 = action.Data as UnbanAuditLogData;
                            if (act4.Target.Id != user.Id)
                                continue;
                            break;
                        case ActionType.MemberUpdated:
                            var act5 = action.Data as MemberUpdateAuditLogData;
                            if (act5.Target.Id != user.Id)
                                continue;
                            break;
                        case ActionType.MemberRoleUpdated:
                            var act6 = action.Data as MemberRoleAuditLogData;
                            if (act6.Target.Id != user.Id)
                                continue;
                            break;
                        case ActionType.MessageDeleted:
                            var act7 = action.Data as MessageDeleteAuditLogData;
                            if (act7.Target.Id != user.Id)
                                continue;
                            break;
                        default:
                            continue;
                    }

                    procActions.Add(action);
                }

                var s = ProcessActions(procActions, amount);

                await ReplyAsync(s);
            }

            private string ProcessActions(IEnumerable<RestAuditLogEntry> allActions, int amount)
            {
                var sb = new StringBuilder();

                sb.AppendLine("```less");

                var counter = 0;

                foreach (var action in allActions)
                {
                    ++counter;

                    switch (action.Action)
                    {
                        case ActionType.Kick:
                            var act1 = action.Data as KickAuditLogData;
                            sb.AppendLine(
                                $"[{action.CreatedAt}] {action.User} Kicked {act1.Target.Id} ({act1.Target})");
                            break;
                        case ActionType.Prune:
                            var act2 = action.Data as PruneAuditLogData;
                            sb.AppendLine($"[{action.CreatedAt}] {action.User} Pruned {act2.MembersRemoved} members");
                            break;
                        case ActionType.Ban:
                            var act3 = action.Data as BanAuditLogData;
                            sb.AppendLine(
                                $"[{action.CreatedAt}] {action.User} Banned {act3.Target.Id} ({act3.Target})");
                            break;
                        case ActionType.Unban:
                            var act4 = action.Data as UnbanAuditLogData;
                            sb.AppendLine(
                                $"[{action.CreatedAt}] {action.User} Unbanned {act4.Target.Id} ({act4.Target})");
                            break;
                        case ActionType.MemberUpdated:
                            var act5 = action.Data as MemberUpdateAuditLogData;

                            var before = new StringBuilder();

                            if (act5.Before.Mute != null)
                                before.Append("Muted: " + act5.Before.Mute +
                                              (act5.Before.Deaf != null || act5.Before.Nickname != null ? ", " : ""));
                            if (act5.Before.Deaf != null)
                                before.Append(
                                    "Deafen: " + act5.Before.Deaf + (act5.Before.Nickname != null ? ", " : ""));
                            if (act5.Before.Nickname != null)
                                before.Append("Nickname: " + act5.Before.Nickname);

                            var after = new StringBuilder();

                            if (act5.After.Mute != null)
                                after.Append("Muted: " + act5.After.Mute +
                                             (act5.After.Deaf != null || act5.After.Nickname != null ? ", " : ""));
                            if (act5.After.Deaf != null)
                                after.Append("Deafen: " + act5.After.Deaf + (act5.After.Nickname != null ? ", " : ""));
                            if (act5.After.Nickname != null)
                                after.Append("Nickname: " + act5.After.Nickname);

                            sb.AppendLine(
                                $"[{action.CreatedAt}] {action.User} Updated {act5.Target} ({before}) -> ({after})");
                            break;
                        case ActionType.MemberRoleUpdated:
                            var act6 = action.Data as MemberRoleAuditLogData;

                            var sb2 = new StringBuilder();

                            foreach (var role in act6.Roles)
                            {
                                sb2.Append(role.Added ? "added " : "removed ");
                                sb2.Append(role.Name);
                            }

                            sb.AppendLine(
                                $"[{action.CreatedAt}] {action.User} Updated {act6.Target}'s role(s) ({sb2})");
                            break;
                        case ActionType.MessageDeleted:
                            var act7 = action.Data as MessageDeleteAuditLogData;
                            sb.AppendLine(
                                $"[{action.CreatedAt}] {action.User} Deleted {act7.MessageCount} messages in #{Context.Guild.GetChannel(act7.ChannelId)} from {Context.Guild.GetUser(act7.Target.Id)}");
                            break;
                        default:
                            --counter;
                            break;
                    }

                    if (counter >= amount)
                        break;
                }

                if (sb.Length == 7 + Environment.NewLine.Length)
                    sb.Append("No data available. Try to specify page.");

                sb.Append("```");

                return sb.ToString();
            }
        }

        [Name("Cleaner")]
        [Group("clean")]
        public class Cleaner : ModuleBase<SocketCommandContext>
        {
            private readonly AppConfiguration _config;

            public Cleaner(AppConfiguration config)
            {
                _config = config;
            }

            [Command("self")]
            [Summary("Cleans bot's messages in current channel.")]
            public async Task CleanSelf([Summary("The maximum messages to view for deletion.")] int max)
            {
                await CleanAsync(Context.Channel,
                    message => message.Author.Id == Context.Client.CurrentUser.Id ||
                               message.MentionedUserIds.Any(x => x == Context.Client.CurrentUser.Id) ||
                               message.Content.StartsWith(_config["prefix"]), max);
            }

            [Command("user")]
            [Summary("Cleans user's messages in current channel.")]
            public async Task CleanUser([Summary("The maximum messages to view for deletion.")] int max,
                [Summary("The user.")] IUser user)
            {
                await CleanAsync(Context.Channel,
                    message => message.Author.Id == user.Id || message.MentionedUserIds.Any(x => x == user.Id), max);
            }

            [Command]
            public async Task Clean([Summary("The maximum messages to view for deletion.")] int max)
            {
                await CleanAsync(Context.Channel, message => true, max);
            }

            private async Task CleanAsync(ISocketMessageChannel channel, Predicate<IMessage> predicate, int max)
            {
                var msgTask = await ReplyAsync("Analyzing (can be slow) ...");

                var messages = Context.Channel.GetMessagesAsync(msgTask, Direction.Before, max + 1);

                var found = new List<IMessage>();

                var now = DateTimeOffset.Now;

                var i = 0;

                await foreach (var l in messages)
                foreach (var message in l)
                {
                    var res = predicate.Invoke(message);
                    if (!res)
                        continue;

                    var days = (now - message.Timestamp).TotalDays;

                    if (days >= 14)
                    {
                        await Context.Channel.DeleteMessageAsync(message);
                        await Task.Delay(1050);

                        ++i;

                        if (i % 10 == 0)
                            await msgTask.ModifyAsync(properties =>
                                properties.Content = new Optional<string>($"Removed {i} messages"));
                    }
                    else
                    {
                        found.Add(message);
                    }
                }

                await msgTask.ModifyAsync(properties =>
                    properties.Content = new Optional<string>("Removed all messages"));

                var t = ReplyAsync("Cleaned!");

                if (found.Count != 0)
                    await ((ITextChannel)channel).DeleteMessagesAsync(found);

                await t;
            }
        }
    }
}

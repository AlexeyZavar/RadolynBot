#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
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

        [Name("Audit")]
        [Group("audit")]
        public class AuditModule : ModuleBase<SocketCommandContext>
        {
            [Command("what")]
            [Summary("Prints latest actions from specified user.")]
            public async Task WhatAsync([Summary("The user.")] IUser user,
                [Summary("The amount of logs.")] int amount = 10, [Summary("The page.")] int page = 1)
            {
                var allActions = await Context.Guild.GetAuditLogsAsync(250 * page, userId: user.Id).FlattenAsync();

                var s = ProcessActions(allActions, amount);

                await ReplyAsync(s);
            }

            [Command("who")]
            [Summary("Prints latest actions with specified user.")]
            public async Task WhoAsync([Summary("The user.")] IUser user,
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
                            if (act7.AuthorId != user.Id)
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
                                $"[{action.CreatedAt}] {action.User} Deleted {act7.MessageCount} messages in #{Context.Guild.GetChannel(act7.ChannelId)} from {Context.Guild.GetUser(act7.AuthorId)}");
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

                await channel.DeleteMessagesAsync(found);

                await ReplyAsync("Cleaned!");
            }
        }
    }
}
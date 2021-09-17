#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using RadLibrary;
using RadLibrary.Configuration;
using Serilog;

#endregion

namespace RadBot.Modules
{
    [RequireOwner]
    [Name("Fun")]
    [Group("fun")]
    public class FunModule : ModuleBase<SocketCommandContext>
    {
        private static int _flightNumber;
        private static readonly List<ulong> InFlight = new List<ulong>();

        private static HashSet<ulong> _inLock = new HashSet<ulong>();
        private static readonly Random Random = new Random();
        
        public FunModule(DiscordSocketClient client)
        {
            client.MessageReceived += ClientOnMessageReceived;
        }

        private static async Task ClientOnMessageReceived(SocketMessage msg)
        {
            if (_inLock.Contains(msg.Author.Id))
                await msg.Channel.DeleteMessageAsync(msg);
        }

        [Command("travel", RunMode = RunMode.Async)]
        [Alias("tp")]
        [Summary("Teleports user around all voice channels.")]
        public async Task Travel([Remainder] [Summary("The user to tp")] IUser user)
        {
            Helper.CheckIgnoreThrow(user);
            var channels = Context.Guild.VoiceChannels;

            var victim = Context.Guild.GetUser(user.Id);

            if (InFlight.Contains(victim.Id))
            {
                await ReplyAsync(user.Mention + " is already in flight.");
                return;
            }

            InFlight.Add(victim.Id);

            if (victim.VoiceChannel == null)
            {
                await ReplyAsync(user.Mention + " is not in the voice channel.");
                return;
            }

            await ReplyAsync(user.Mention + " tobi pizda. Flex Airlines flight #" + ++_flightNumber);

            await victim.ModifyAsync(properties =>
            {
                properties.Deaf = true;
                properties.Mute = true;
            });

            var saveChannel = victim.VoiceChannel;

            foreach (var channel in channels)
            {
                // перед каждым перемещеним запрашиваем жертву
                victim = Context.Guild.GetUser(user.Id);

                // если не в голосовом канале, то в цикле запрашиваем инфу о пользователе и проверяем, есть ли он в войсе
                while (victim.VoiceChannel == null)
                {
                    victim = Context.Guild.GetUser(user.Id);
                    await Task.Delay(750);
                }

                // перемещаем
                await victim.ModifyAsync(properties => properties.Channel = channel);
                // пауза
                await Task.Delay(750);
            }

            victim = Context.Guild.GetUser(user.Id);

            if (victim.VoiceChannel != null)
                await victim.ModifyAsync(properties =>
                {
                    properties.Channel = saveChannel;
                    properties.Deaf = false;
                    properties.Mute = false;
                });

            InFlight.Remove(victim.Id);

            await ReplyAsync(user.Mention + ", Flex Airlines flight #" + _flightNumber + " finished successfully.");
        }

        [Command("lock", RunMode = RunMode.Async)]
        [Summary("Prevents user from connecting to any voice channel.")]
        public async Task Lock([Summary("The user to lock.")] IUser user,
            [Summary("The time to lock for.")] TimeSpan time = default)
        {
            if (!Helper.CheckIgnoreBool(user))
                return;

            if (time == default)
                time = new TimeSpan(TimeSpan.TicksPerDay);

            var victim = Context.Guild.GetUser(user.Id);

            if (_inLock.Contains(victim.Id))
            {
                await ReplyAsync(user.Mention + " is already in lock.");
                return;
            }

            _inLock.Add(user.Id);

            await ReplyAsync(user.Mention + ", you're locked for " + time.TotalSeconds + " seconds.");

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < time.TotalMilliseconds && _inLock.Contains(user.Id))
            {
                victim = Context.Guild.GetUser(user.Id);

                if (victim.VoiceChannel != null)
                    await victim.ModifyAsync(properties =>
                    {
                        properties.Channel = null;
                        properties.Deaf = true;
                        properties.Mute = true;
                    });

                await Task.Delay(750);
            }

            _inLock.Remove(user.Id);

            await ReplyAsync(user.Mention + ", you can join now.");

            SpinWait.SpinUntil(() =>
            {
                victim = Context.Guild.GetUser(user.Id);
                return victim.VoiceChannel != null;
            }, 30000);

            if (victim.VoiceChannel != null)
                await victim.ModifyAsync(properties =>
                {
                    properties.Deaf = false;
                    properties.Mute = false;
                });
        }

        [Command("lock", RunMode = RunMode.Async)]
        [Summary("Prevents users from connecting to any voice channel.")]
        public Task Lock([Summary("The time to lock for.")] TimeSpan time = default)
        {
            if (time == default)
                time = new TimeSpan(TimeSpan.TicksPerDay);

            var victims = Context.Guild.GetUser(Context.User.Id).VoiceChannel;

            foreach (var socketGuildUser in victims.Users)
            {
                Task.Run(() => Lock(socketGuildUser, time));
                Task.Delay(750);
            }

            return Task.CompletedTask;
        }

        [Command("unlock")]
        [Summary("Allows user to join voice channels again.")]
        public async Task Unlock([Summary("The user to unlock.")] IUser user = default)
        {
            if (_inLock.Contains(Context.User.Id))
            {
                await ReplyAsync(Context.User.Mention + ", you cannot unlock anyone while you're in lock.");
                return;
            }

            if (user == default)
            {
                _inLock = new HashSet<ulong>();
                return;
            }

            if (!_inLock.Contains(user.Id))
            {
                await ReplyAsync(user.Mention + " is not locked.");
                return;
            }

            _inLock.Remove(user.Id);
        }

        [Command("spam", RunMode = RunMode.Async)]
        [Summary("Spams message.")]
        public async Task Spam([Summary("How many times to repeat.")] int times,
            [Remainder] [Summary("The message.")] string msg)
        {
            for (var i = 0; i < times; i++)
            {
                await ReplyAsync(msg);
                await Task.Delay(500);
            }
        }

        [Command("meme")]
        [Summary("Prints meme (Russian).")]
        public async Task RussianMeme()
        {
            var wc = new WebClient();

            var s = wc.DownloadString("http://fucking-great-advice.ru/api/random");

            var meme = JsonConvert.DeserializeObject<Meme>(s);

            await ReplyAsync(meme.Text);
        }

        [Command("randomperson")]
        [Summary("Picks a random person from current channel.")]
        public async Task RandomPerson()
        {
            var users = (await Context.Channel.GetUsersAsync().FlattenAsync()).ToList();

            var num = Random.Next(users.Count);

            await ReplyAsync(users[num].Username);
        }

        [Command("fuck")]
        [Summary("Spam to PM.")]
        public async Task Fuck(IUser user, [Remainder] string message)
        {
            var channel = await user.GetOrCreateDMChannelAsync();

            await channel.SendMessageAsync(message);
        }

        [Command("joinspam", RunMode = RunMode.Async)]
        [Summary("Voice join\\disconnect spam.")]
        public async Task JoinSpam([Summary("The voice channel.")] SocketVoiceChannel channel,
            [Summary("How many times to repeat.")] int times)
        {
            for (var i = 0; i < times; ++i)
                try
                {
                    await channel.ConnectAsync();
                    await Task.Delay(800);
                    try
                    {
                        await Context.Guild.CurrentUser.VoiceChannel.DisconnectAsync();
                    }
                    catch
                    {
                    }

                    await Task.Delay(800);
                }
                catch
                {
                }
        }

        [Command("joinspam", RunMode = RunMode.Async)]
        [Summary("Voice join\\disconnect spam.")]
        public async Task JoinSpam([Summary("The voice channel.")] IGuildUser user,
            [Summary("How many times to repeat.")] int times)
        {
            for (var i = 0; i < times; ++i)
                try
                {
                    var channel = user.VoiceChannel;

                    while (channel == null) channel = user.VoiceChannel;

                    await channel.ConnectAsync();
                    await Task.Delay(800);
                    try
                    {
                        await Context.Guild.CurrentUser.VoiceChannel.DisconnectAsync();
                    }
                    catch
                    {
                    }

                    await Task.Delay(800);
                }
                catch
                {
                }
        }
        
        [Name("Raid")]
        [Group("raid")]
        private class RaidModule : ModuleBase<SocketCommandContext>
        {
            protected override async void BeforeExecute(CommandInfo command)
            {
                await Context.Message.DeleteAsync();

                base.BeforeExecute(command);
            }

            [Command("verify", RunMode = RunMode.Async)]
            [Alias("ensure")]
            [Summary("Verifies that we can raid this channel.")]
            public async Task Verify()
            {
                var bot = Context.Guild.GetUser(Context.Client.CurrentUser.Id);

                var text = ", I'm ready.";

                // does our bot has permissions to create channels
                if (!bot.GuildPermissions.ManageChannels) text = ", I have no permissions to manage channels.";

                var msg = await ReplyAsync(Context.User.Mention + text);
                await Task.Delay(1500);
                await msg.DeleteAsync();
            }

            [Command("flood", RunMode = RunMode.Async)]
            [Summary("Creates many voice channels.")]
            public async Task VoiceFlood()
            {
                for (var i = 0; i < int.MaxValue; ++i)
                    try
                    {
                        await Context.Guild.CreateVoiceChannelAsync(i.ToString());
                        Log.Information("CREATED {Count} VOICE CHANNEL", i);
                    }
                    catch
                    {
                        Log.Warning("FAILED TO CREATE {Count} VOICE CHANNEL", i);
                    }
            }

            [Command("unflood", RunMode = RunMode.Async)]
            [Summary("Removes flooded voice channels.")]
            public async Task VoiceUnFlood()
            {
                var channels = Context.Guild.VoiceChannels;

                foreach (var channel in channels)
                    if (int.TryParse(channel.Name, out _))
                        try
                        {
                            await channel.DeleteAsync();
                            Log.Information("DELETED {ChannelName} VOICE CHANNEL", channel.Name);
                        }
                        catch
                        {
                            Log.Warning("FAILED TO DELETE {ChannelName} VOICE CHANNEL", channel.Name);
                        }
            }

            [Command("unflood", RunMode = RunMode.Async)]
            [Summary("Removes voice channels by regex.")]
            public async Task VoiceUnFlood([Remainder] string regex)
            {
                var channels = Context.Guild.VoiceChannels;

                var regexExp = new Regex(regex);

                foreach (var channel in channels.Where(x => regexExp.IsMatch(x.Name)))
                    try
                    {
                        await channel.DeleteAsync();
                        Log.Information("DELETED {ChannelName} VOICE CHANNEL", channel.Name);
                    }
                    catch
                    {
                        Log.Warning("FAILED TO DELETE {ChannelName} VOICE CHANNEL", channel.Name);
                    }
            }

            [Command("nick", RunMode = RunMode.Async)]
            [Summary("Sets all nicknames of current guild members to provided.")]
            public async Task Nick([Remainder] [Summary("The nickname.")] string nick)
            {
                var users = Context.Guild.Users;

                var botRole = Context.Guild.CurrentUser.Roles.Max(x => x.Position);

                foreach (var user in users.Where(user => !user.Roles.Any(x => x.Position >= botRole)))
                    try
                    {
                        await user.ModifyAsync(properties => properties.Nickname = nick);
                        Log.Information("Set nickname for {Username}", user.Username);
                    }
                    catch
                    {
                        Log.Warning("Failed to set nickname for {Username}", user.Username);
                    }

                var msg = await ReplyAsync("Done!");
                await Task.Delay(1500);
                await msg.DeleteAsync();
            }

            [Command("nicks", RunMode = RunMode.Async)]
            [Summary("Sets all nicknames of current guild members to provided.")]
            public async Task Nicks([Summary("The nicknames.")] params string[] nicks)
            {
                var users = Context.Guild.Users;

                foreach (var user in users)
                    try
                    {
                        await user.ModifyAsync(properties => properties.Nickname = nicks.RandomItem());
                        Log.Information("Set nickname for {Username}", user.Username);
                    }
                    catch
                    {
                        Log.Warning("Failed to set nickname for {Username}", user.Username);
                    }

                var msg = await ReplyAsync("Done!");
                await Task.Delay(1500);
                await msg.DeleteAsync();
            }

            [Command("unnick", RunMode = RunMode.Async)]
            [Summary("Restores all nicks to default.")]
            public async Task UnNick()
            {
                var users = Context.Guild.Users;

                foreach (var user in users)
                    try
                    {
                        await user.ModifyAsync(properties => properties.Nickname = null);
                        Log.Information("Restored nickname for {Username}", user.Username);
                    }
                    catch
                    {
                        Log.Warning("Failed to restore nickname for {Username}", user.Username);
                    }

                var msg = await ReplyAsync("Done!");
                await Task.Delay(1500);
                await msg.DeleteAsync();
            }
        }

        private sealed class Meme
        {
            [JsonProperty("id")] public long Id { get; set; }

            [JsonProperty("text")] public string Text { get; set; }

            [JsonProperty("sound")] public string Sound { get; set; }
        }
    }
}

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using RadBot.Modules.Helpers.Meowpad;
using RadLibrary;
using RadLibrary.Configuration;

#endregion

namespace RadBot.Modules
{
    [Name("SoundPad")]
    [Group("sp")]
    public sealed class SoundPadModule : ModuleBase<SocketCommandContext>
    {
        private static readonly Dictionary<ulong, CancellationTokenSource> Tokens =
            new();

        private readonly AppConfiguration _config;

        public SoundPadModule(AppConfiguration config)
        {
            _config = config;
        }

        protected override async void BeforeExecute(CommandInfo command)
        {
            await Context.Message.DeleteAsync();

            base.BeforeExecute(command);
        }

        private async void SendError(string message)
        {
            var msg = await ReplyAsync(message);
            await Task.Delay(1500);
            await msg.DeleteAsync();
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Plays specified sound.")]
        public async Task Play([Summary("The channel to play in")] SocketVoiceChannel channel,
            [Remainder] [Summary("The sound to play.")]
            string sound)
        {
            // if user not in voice channel
            if (channel == null)
            {
                SendError(Context.User.Mention + ", you're not in channel.");
                return;
            }

            var file = GetFile(sound);

            if (file == null)
            {
                SendError(Context.User.Mention + ", sound not found.");
                return;
            }

            var voice = await ConnectToVoice(channel);

            var task = Task.Run(() => PlaySound(voice, file));

            while (!task.IsCompleted)
            {
                try
                {
                    await Context.Guild.CurrentUser.ModifyAsync(properties =>
                        properties.Mute = new Optional<bool>(false));
                }
                catch
                {
                    // ok
                    break;
                }


                await Task.Delay(750);
            }

            await Task.Delay(650);

            if (!Tokens[Context.Guild.Id].IsCancellationRequested)
                await channel.DisconnectAsync();
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Plays specified sound.")]
        public async Task Play([Remainder] [Summary("The sound to play.")] string sound)
        {
            var channel = Context.Guild.GetUser(Context.User.Id).VoiceChannel;

            // if user not in voice channel
            if (channel == null)
            {
                SendError(Context.User.Mention + ", you're not in channel.");
                return;
            }

            await Play(channel, sound);
        }

        [Command("fetch", RunMode = RunMode.Async)]
        [Alias("download", "meowpad", "meow")]
        [Summary("Downloads specified sound from https://meowpad.me.")]
        public async Task Fetch([Remainder] [Summary("The sound to download.")] string sound)
        {
            var sounds = await MeowpadParser.FetchSound(sound, 1);

            if (sounds.Meta.TotalResults == 0)
            {
                SendError("No sounds found.");
                return;
            }

            if (sounds.Meta.TotalResults == 1)
            {
                var msg = await ReplyAsync("Downloading...");

                var res = await MeowpadParser.DownloadSound(sounds.Sounds[0].Slug, _config["soundPadPath"]);

                await msg.ModifyAsync(properties => properties.Content = res ? "Downloaded!" : "Failed to download!");

                return;
            }

            var builder = Helper.GetBuilder();

            builder.Title = "Sounds list";

            foreach (var fSound in sounds.Sounds)
                builder.AddField($"{fSound.Title} ({fSound.Id})", "Downloads: " + fSound.Downloads);

            await ReplyAsync(embed: builder.Build());
        }

        [Command("fetch", RunMode = RunMode.Async)]
        [Alias("download", "meowpad", "meow")]
        [Summary("Downloads specified sound from https://meowpad.me.")]
        public async Task Fetch([Remainder] [Summary("The id of sound to download.")] int id)
        {
            var sounds = await MeowpadParser.FetchSoundById(id);

            if (sounds.Id == 0)
            {
                SendError("Sound not found.");
                return;
            }

            var msg = await ReplyAsync("Downloading...");

            var res = await MeowpadParser.DownloadSound(sounds.Slug, _config["soundPadPath"]);

            await msg.ModifyAsync(properties => properties.Content = res ? "Downloaded!" : "Failed to download!");
        }

        [Command("list")]
        [Summary("Prints all available sounds.")]
        public async Task List()
        {
            var files = Directory.EnumerateFiles(_config["soundPadPath"]);

            var embedBuilder = Helper.GetBuilder();

            embedBuilder.Title = "SoundPad";

            var sb = new StringBuilder();

            var i = 1;

            foreach (var file in files)
            {
                var name = _config["bulletSymbol"] + " " + Path.GetFileNameWithoutExtension(file) + Environment.NewLine;

                if (name.Length + sb.Length >= 1024)
                {
                    embedBuilder.AddField($"Available sounds ({i++})", sb.ToString());
                    sb.Clear();
                }

                sb.Append(name);
            }

            if (sb.Length != 0) embedBuilder.AddField($"Available sounds ({i++})", sb);

            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("volume", RunMode = RunMode.Async)]
        [Alias("vol")]
        [Summary("Prints volume.")]
        public async Task GetVolume()
        {
            var msg = await ReplyAsync($"Current volume multiplier: {_config["soundPadVolume"]}x");

            await Task.Delay(1000);

            await msg.DeleteAsync();
        }

        [Command("volume")]
        [Alias("vol")]
        [Summary("Sets volume.")]
        public Task SetVolume([Remainder] [Summary("The volume.")] int volume)
        {
            _config["soundPadVolume"] = volume.ToString();

            return Task.CompletedTask;
        }

        private async Task PlaySound(IAudioClient voice, string file)
        {
            try
            {
                // create audio streams
                var stream = voice.CreatePCMStream(AudioApplication.Mixed, null, 200);
                var soundStream = CreateStream(file);

                // copy audio from ffmpeg to pcm stream
                await soundStream.StandardOutput.BaseStream.CopyToAsync(stream, Tokens[Context.Guild.Id].Token);

                await stream.FlushAsync(Tokens[Context.Guild.Id].Token);
            }
            catch (HttpException)
            {
                // ok (disconnected)
            }
        }

        private async Task<IAudioClient> ConnectToVoice(IAudioChannel channel)
        {
            IAudioClient voice;

            var current = Context.Guild.GetUser(Context.Client.CurrentUser.Id).VoiceChannel;

            // connect if not connected
            if (current?.Id != channel.Id)
            {
                voice = await channel.ConnectAsync(true);
            }
            else // if already in channel, cancel sound that playing right now
            {
                voice = Context.Guild.AudioClient;
                Tokens[Context.Guild.Id].Cancel(true);
                await Task.Delay(100);
            }

            // set cancellation token
            if (Tokens.ContainsKey(Context.Guild.Id))
                Tokens[Context.Guild.Id] = new CancellationTokenSource();
            else
                Tokens.Add(Context.Guild.Id, new CancellationTokenSource());

            // un mute bot
            await Context.Guild.GetUser(Context.Client.CurrentUser.Id)
                .ModifyAsync(properties => properties.Mute = false);

            // get full path to first sound that matches string
            return voice;
        }

        private string GetFile(string sound)
        {
            // find all sounds matches string
            var files = Directory.GetFiles(_config["soundPadPath"], sound + "*");

            // if not found
            if (files.Length == 0) return null;

            return Path.GetFullPath(Path.Combine(Utilities.IsWindows ? _config["soundPadPath"] : ".", files[0]));
        }

        private Process CreateStream(string path)
        {
            var isWin = Utilities.IsWindows;

            var args =
                $"-loglevel fatal -i \"{path}\" -filter:a \"volume={_config["soundPadVolume"]}\" -ac 2 -f s16le -ar 48000 pipe:1";

            return Process.Start(new ProcessStartInfo
            {
                FileName = isWin ? Path.Combine("Binaries", "FFmpeg", "win", "ffmpeg.exe") : "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
    }
}

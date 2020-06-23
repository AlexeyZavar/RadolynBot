#region

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using NYoutubeDL;
using RadLibrary;
using RadLibrary.Configuration;
using RadLibrary.Logging;

#endregion

namespace RadBot.Modules
{
    [Name("Player")]
    [Group("player")]
    public class PlayerModule : ModuleBase<SocketCommandContext>
    {
        // todo: save
        //private readonly Dictionary<SocketGuild, GuildMusicConfiguration> _configurations = new Dictionary<SocketGuild, GuildMusicConfiguration>();
        private readonly AppConfiguration _config;

        public PlayerModule(AppConfiguration config)
        {
            _config = config;
        }

        [Command("join", RunMode = RunMode.Async)]
        [Summary("Joins in current voice channel.")]
        public async Task JoinAsync()
        {
            var user = Context.Guild.GetUser(Context.User.Id);

            if (user.VoiceChannel == null)
            {
                await ReplyAsync(user.Mention + ", you're not in voice channel.");
                return;
            }

            if (Context.Guild.GetUser(Context.Client.CurrentUser.Id).VoiceChannel != null)
            {
                await ReplyAsync(user.Mention + ", I'm already in the voice channel.");
                return;
            }

            var audioClient = await user.VoiceChannel.ConnectAsync();

            if (audioClient == null)
            {
                await ReplyAsync("Something went wrong.");
            }

            /*
            var stream = audioClient.CreatePCMStream(AudioApplication.Mixed);
            var joinSound = CreateStream(Path.GetFullPath(Path.Combine("Assets", "join.mp3")));

            await joinSound.StandardOutput.BaseStream.CopyToAsync(stream);
            
            await stream.FlushAsync();
            */
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Joins in current voice channel.")]
        public async Task PlayAsync([Remainder] [Summary("The url.")] string url)
        {
            var audioClient = Context.Guild.AudioClient;

            if (audioClient == null)
            {
                await ReplyAsync("I'm not in the voice channel.");
                return;
            }

            var stream = audioClient.CreatePCMStream(AudioApplication.Music);

            var statusMsg = await ReplyAsync("Caching...");

            var name = Helper.RandomString(16);

            var youtubeDl = new YoutubeDL
            {
                YoutubeDlPath = _config["youtube-dl"]
            };

            var path = Path.Combine("Assets", "Downloads", name);

            Directory.CreateDirectory(path);

            youtubeDl.Options.FilesystemOptions.Output = Path.Combine(path, "%(autonumber)s.%(title)s.%(ext)s");
            youtubeDl.VideoUrl = url;

            youtubeDl.StandardOutputEvent += (sender, s) => LogManager.GetClassLogger().Info(s);
            youtubeDl.StandardErrorEvent += (sender, s) => LogManager.GetClassLogger().Error(s);

            await youtubeDl.DownloadAsync();

            if (youtubeDl.Info.Errors.Count != 0)
            {
                await statusMsg.ModifyAsync(msg => msg.Content = "Failed to retrieve information.");
                return;
            }

            await statusMsg.ModifyAsync(msg => msg.Content = "Cached. Starting playback...");

            var files = Directory.EnumerateFiles(path);

            foreach (var file in files)
            {
                var joinSound = CreateStream(file);

                await statusMsg.ModifyAsync(msg =>
                    msg.Content = "Now playing: " + Path.GetFileNameWithoutExtension(file));

                await joinSound.StandardOutput.BaseStream.CopyToAsync(stream);

                await stream.FlushAsync();
            }
        }

        [Command("leave", RunMode = RunMode.Async)]
        [Alias("disconnect", "dc")]
        [Summary("Leaves from channel and cleans queue.")]
        public async Task LeaveAsync()
        {
            var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);

            if (user.VoiceChannel == null) await ReplyAsync("I'm not in the voice channel.");

            await Context.Guild.AudioClient.StopAsync();
        }

        private static Process CreateStream(string path)
        {
            var isWin = Utilities.IsWindows();

            path = $"-loglevel fatal -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1";

            return Process.Start(new ProcessStartInfo
            {
                // todo: remove cmd
                FileName = "cmd",
                Arguments = "/c " + Path.Combine("Binaries", "FFmpeg", isWin ? "win" : "linux",
                    isWin ? "ffmpeg.exe" : "ffmpeg") + " " + path + " && exit",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
        
        // private sealed class GuildMusicConfiguration
        // {
        //     public bool IsPaused;
        //     public int Volume;
        //     public Queue<string> Queue;
        // }
    }
}
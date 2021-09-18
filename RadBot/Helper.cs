#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using RadLibrary;
using RadLibrary.Colors;
using RadLibrary.Configuration;
using RadLibrary.Configuration.Scheme;
using Serilog;

#endregion

namespace RadBot
{
    public static class Helper
    {
        private const string Version = "0.6";

        private static DateTime _botStartTime;
        private static AppConfiguration _config;

        private static readonly HashSet<ulong> IgnoredUsers = new() { 305414308320247818 };

        private static readonly List<string> Gifs = new()
        {
            "https://c.tenor.com/VrfSZUjiWn4AAAAC/shy-anime.gif",
            "https://c.tenor.com/NnNnxJlJhc8AAAAC/shy-anime.gif",
            "https://c.tenor.com/BBuE8xkVCHgAAAAC/anime-blush.gif",
            "https://c.tenor.com/uT9BWeRBJwYAAAAC/blushing-anime-girl.gif",
            "https://c.tenor.com/6xnCJNAGXN8AAAAC/blushes-anime-girl-blush.gif",
            "https://c.tenor.com/hXGbCYQfO6oAAAAC/anime-blush.gif"
        };

        public static HttpClient HttpClient { get; } = new();

        public static double UpTime => (DateTime.Now - _botStartTime).TotalSeconds;

        public static void IsUserIgnoredThrow(IUser user)
        {
            if (IgnoredUsers.Contains(user.Id))
                throw new Exception($"{user.Username} is in ignore list");
        }

        public static async Task<string> ShortenUrl(string url)
        {
            var res = await HttpClient.PostAsJsonAsync("https://gotiny.cc/api", new { input = url });
            var doc = await res.Content.ReadFromJsonAsync<JsonDocument>();

            return "https://gotiny.cc/" + doc.RootElement[0].GetProperty("code").GetString();
        }

        public static async Task Initialize(AppConfiguration config)
        {
            _botStartTime = DateTime.Now;

            var oldPath = Environment.GetEnvironmentVariable("PATH");

            // path to some libs
            Environment.SetEnvironmentVariable("PATH", oldPath + Path.PathSeparator + BinaryHelper.BinariesPath);

            var executablesTask = BinaryHelper.CheckExecutables();

            // init config
            ConfigurationScheme.Ensure(config, typeof(Config));

            // check essential settings
            if (string.IsNullOrWhiteSpace(config["token"]) ||
                string.IsNullOrWhiteSpace(config["prefix"]) ||
                string.IsNullOrWhiteSpace(config["builderColor"]))
            {
                Log.Fatal("Bot token\\prefix\\builderColor is not specified in bot.conf!");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            config.HotReload = true;
            _config = config;

            await executablesTask;

            Log.Information("PATH: {Path}", Environment.GetEnvironmentVariable("PATH"));
        }

        private static Color GetEmbedColor()
        {
            var color = Colorizer.HexToColor(_config["builderColor"]);
            return new Color(color.R, color.G, color.B);
        }

        public static EmbedBuilder GetBuilder()
        {
            var embedBuilder = new EmbedBuilder()
                .WithColor(GetEmbedColor())
                .WithFooter("Sistine Legacy by Radolyn (v" + Version + " by AlexeyZavar#2198)",
                    "https://radolyn.com/shared/2.jpg")
                .WithImageUrl(Gifs.RandomItem());

            return embedBuilder;
        }
    }
}

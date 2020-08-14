#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using RadLibrary;
using RadLibrary.Colors;
using RadLibrary.Configuration;
using RadLibrary.Configuration.Scheme;
using RadLibrary.Logging;

#endregion

namespace RadBot
{
    public static class Helper
    {
        public static DateTime BotStartedTime;

        public static double UpTime => (DateTime.Now - BotStartedTime).TotalMilliseconds;

        public static string AppPath;

        public const string Version = "0.3";

        private static AppConfiguration _config;

        private static readonly Random _random = new Random();

        private static readonly HashSet<ulong> _ignoreUsers = new HashSet<ulong> {305414308320247818};

        public static void CheckIgnoreThrow(IUser user)
        {
            if (_ignoreUsers.Contains(user.Id))
                throw new Exception();
        }

        /// <summary>
        ///     Returns false if in ignore list
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool CheckIgnoreBool(IUser user)
        {
            return !_ignoreUsers.Contains(user.Id);
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        public static Task Initialize(AppConfiguration config)
        {
            BotStartedTime = DateTime.Now;
            AppPath = new Uri(typeof(Config).Assembly.CodeBase!).LocalPath;

            var oldPath = Environment.GetEnvironmentVariable("PATH");

            // path to some libs
            Environment.SetEnvironmentVariable("PATH",
                oldPath + ";" + Path.Combine("Binaries", "Libs") + ";" +
                Path.Combine("Binaries", "FFmpeg", Utilities.IsWindows() ? "win" : "linux"));

            // init config
            ConfigurationScheme.Ensure(config, typeof(Config));

            // check essential settings
            if (config["token"] == "" || config["prefix"] == "" || config["builderColor"] == "")
            {
                LogManager.GetMethodLogger().Fatal("Bot token\\prefix\\builderColor not specified in bot.conf!");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            config.HotReload = true;

            _config = config;


            return Task.CompletedTask;
        }

        public static string FormatException(Exception e)
        {
            return Environment.NewLine + e?.Source + ": " + e?.GetType() + Environment.NewLine + "Message: " +
                   e?.Message +
                   Environment.NewLine + "Stack trace:" + Environment.NewLine +
                   Sub(e?.StackTrace, 1800);
        }

        private static string Sub(string s, int length)
        {
            if (s == null)
                return "<nah>";

            if (s.Length <= length)
                return s;
            return s.Substring(0, length);
        }

        private static Color GetEmbedColor()
        {
            var color = Colorizer.HexToColor(_config["builderColor"]);
            return new Color(color.R, color.G, color.B);
        }

        public static EmbedBuilder GetBuilder()
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder.WithColor(GetEmbedColor());
            embedBuilder.WithFooter("RadBot by Radolyn (v" + Version + " by AlexeyZavar#2198)",
                "https://radolyn.com/img/rd.png");

            // todo: random gif

            return embedBuilder;
        }
    }
}
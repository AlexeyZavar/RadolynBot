#region

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using RadLibrary;
using RadLibrary.Configuration;
using RadLibrary.Logging;

#endregion

namespace RadBot
{
    public static class Helper
    {
        public static DateTime BotStartedTime;

        public static double Uptime => (DateTime.Now - BotStartedTime).TotalMilliseconds;

        public static string AppPath;

        public const string Version = "0.2";

        private static AppConfiguration _config;

        private static readonly Random _random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        public static Task Initialize(AppConfiguration config)
        {
            BotStartedTime = DateTime.Now;
            AppPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;

            // path to some libs
            Environment.SetEnvironmentVariable("PATH", Path.Combine("Binaries", "Libs"));
            
            // path to some executables
            Environment.SetEnvironmentVariable("PATH", Path.Combine("Binaries", "FFmpeg", Utilities.IsWindows() ? "win" : "linux"));

            // init config
            config.SetComment("token", "The bot token");
            config.SetComment("builderColor", "The embed builder's color (hex format)");
            config.SetComment("bulletSymbol", "The bullet symbol (•)");
            config.SetComment("youtube-dl", "The path to youtube-dl");

            config.Save();

            if (config["token"] == "")
            {
                LogManager.GetMethodLogger().Fatal("Bot token not specified in bot.conf!");
                Environment.Exit(-1);
            }

            config.HotReload = true;

            _config = config;


            return Task.CompletedTask;
        }

        public static string FormatException(Exception e)
        {
            return Environment.NewLine + e.Source + ": " + e.GetType() + Environment.NewLine + "Message: " + e.Message +
                   Environment.NewLine + "Stack trace:" + Environment.NewLine +
                   e.StackTrace;
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
            embedBuilder.WithFooter("RadBot by Radolyn (v" + Version + " by AlexeyZavar#2198)");

            return embedBuilder;
        }
    }
}
#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RadLibrary;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

#endregion

namespace RadBot
{
    public static class BinaryHelper
    {
        public static readonly string BinariesPath =
            Path.Combine(Path.GetDirectoryName(typeof(BinaryHelper).Assembly.Location)!, "Binaries",
                Utilities.IsWindows ? "win" : "linux");

        public static async Task CheckExecutables()
        {
            Directory.CreateDirectory(BinariesPath);
            var tasks = new List<Task>(4);

            var ytdlExists = IsExecutableExists("youtube-dl");
            if (!ytdlExists)
                tasks.Add(DownloadYoutubeDl());

            var ffmpegExists = IsExecutableExists("ffmpeg");
            if (!ffmpegExists)
                tasks.Add(DownloadFfmpeg());

            var libsodiumExists = IsLibraryExists("libsodium");
            if (!libsodiumExists)
                tasks.Add(DownloadSodium());

            var opusExists = IsLibraryExists("libopus");
            if (!opusExists)
                tasks.Add(DownloadOpus());

            await Task.WhenAll(tasks);
        }

        private static async Task DownloadOpus()
        {
            // win:   https://dsharpplus.github.io/natives/vnext_natives_win32_x64.zip
            // linux: -

            Log.Information("Downloading libopus");

            if (!Utilities.IsWindows)
            {
                Log.Error("libopus should be installed using package manager");
                return;
            }

            var url = "https://dsharpplus.github.io/natives/vnext_natives_win32_x64.zip";
            var filepath = Path.Combine(BinariesPath, "opus.arc");

            await DownloadFile(url, filepath);
            Extract(filepath, x => x.EndsWith("libopus.dll"));
        }

        private static async Task DownloadSodium()
        {
            // win:   https://download.libsodium.org/libsodium/releases/libsodium-1.0.18-stable-msvc.zip
            // linux: -

            Log.Information("Downloading libsodium");

            if (!Utilities.IsWindows)
            {
                Log.Error("libsodium should be installed using package manager");
                return;
            }

            var url = "https://download.libsodium.org/libsodium/releases/libsodium-1.0.18-stable-msvc.zip";
            var filepath = Path.Combine(BinariesPath, "sodium.arc");

            await DownloadFile(url, filepath);
            Extract(filepath, x =>
                x.Contains("Release") &&
                x.Contains("dynamic") &&
                x.EndsWith(".dll") &&
                !x.Contains("32"));
        }

        private static async Task DownloadFfmpeg()
        {
            // win:   https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip
            // linux: https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz

            Log.Information("Downloading FFmpeg");

            var url = Utilities.IsWindows
                ? "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
                : "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz";
            var filepath = Path.Combine(BinariesPath, "ffmpeg.arc");

            await DownloadFile(url, filepath);
            Extract(filepath, x => x.EndsWith("ffmpeg") || x.EndsWith("ffmpeg.exe"));
        }

        private static async Task DownloadYoutubeDl()
        {
            Log.Information("Downloading youtube-dl");

            var filename = "youtube-dl" + (Utilities.IsWindows ? ".exe" : "");
            var filepath = Path.Combine(BinariesPath, filename);

            await DownloadFile("https://github.com/ytdl-org/youtube-dl/releases/latest/download/" +
                               filename, filepath);
        }

        private static void Extract(string filepath, Predicate<string> pred)
        {
            var archive = ArchiveFactory.Open(filepath);

            var reader = archive.ExtractAllEntries();
            while (reader.MoveToNextEntry())
            {
                if (reader.Entry.IsDirectory) continue;
                if (pred(reader.Entry.Key)) break;
            }


            reader.WriteEntryToDirectory(BinariesPath, new ExtractionOptions
            {
                ExtractFullPath = false,
                Overwrite = true
            });

            reader.Dispose();
            archive.Dispose();

            File.Delete(filepath);

            SetPermissions(Path.Combine(BinariesPath, Path.GetFileName(reader.Entry.Key)));
        }

        private static async Task DownloadFile(string url, string filepath)
        {
            Log.Information("Downloading {Url}", url);

            Directory.CreateDirectory(Path.GetDirectoryName(filepath)!);

            if (File.Exists(filepath))
            {
                Log.Warning("File {Filepath} already exists in system", filepath);
                return;
            }

            await using var stream = await Helper.HttpClient.GetStreamAsync(url);
            await using var f = File.OpenWrite(filepath);

            await stream.CopyToAsync(f);

            Log.Information("{Filename} downloaded", Path.GetFileName(filepath));
            SetPermissions(filepath);
        }

        private static void SetPermissions(string filepath)
        {
            // make +x for linux
            if (!Utilities.IsWindows)
            {
                Log.Information("Setting chmod for {File}", filepath);
                var psi = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = "+x " + filepath,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
        }

        private static bool IsExecutableExists(string filename)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = filename,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                Process.Start(psi)!.Kill(true);
                Log.Information("{Filename} exists in system", filename);
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("Error while trying to run {Filename} ({Exception})", filename, e.Message);
                return false;
            }
        }

        private static bool IsLibraryExists(string libname)
        {
            try
            {
                if (!Utilities.IsWindows)
                    libname += ".so";

                var ptr = NativeLibrary.Load(libname);
                NativeLibrary.Free(ptr);

                Log.Information("{Lib} can be loaded", libname);
                return true;
            }
            catch (Exception e)
            {
                Log.Warning(e, "{Lib} can't be loaded", libname);

                return false;
            }
        }
    }
}

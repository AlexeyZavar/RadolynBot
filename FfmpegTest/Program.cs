using System;
using System.IO;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace FfmpegTest
{
    class Program
    {
        static void Main(string[] args)
        {
            FFmpegDownloader.GetLatestVersion(FFmpegVersion.Full);
            
            FFmpeg.Conversions.New().

            ;
        }
    }
}
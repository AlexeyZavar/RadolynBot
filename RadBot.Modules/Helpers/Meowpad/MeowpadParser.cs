#region

using System.IO;
using System.Net;

#endregion

namespace RadBot.Modules.Helpers.Meowpad
{
    public static class MeowpadParser
    {
        private static readonly WebClient _wc = new WebClient();

        public static MeowpadData FetchSound(string name, int page)
        {
            var data = _wc.DownloadString($"https://api.meowpad.me/v2/sounds/search?q={name}&page={page}&order=date");

            var sounds = MeowpadData.FromJson(data);

            return sounds;
        }

        public static Sound FetchSoundById(int id)
        {
            var data = _wc.DownloadString($"https://api.meowpad.me/v1/sounds/{id}");

            var sound = SoundMeta.FromJson(data);

            return sound.Sound;
        }

        public static bool DownloadSound(string name, string libraryPath)
        {
            var path = Path.Combine(libraryPath, name) + ".mp3";

            if (File.Exists(path)) return false;

            _wc.DownloadFile($"https://api.meowpad.me/v1/download/{name}", path);

            return true;
        }
    }
}

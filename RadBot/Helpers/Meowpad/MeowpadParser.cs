#region

using System.IO;
using System.Threading.Tasks;

#endregion

namespace RadBot.Modules.Helpers.Meowpad
{
    public static class MeowpadParser
    {
        public static async Task<MeowpadData> FetchSound(string name, int page)
        {
            var data = await Helper.HttpClient.GetStringAsync(
                $"https://api.meowpad.me/v2/sounds/search?q={name}&page={page}&order=date");

            var sounds = MeowpadData.FromJson(data);

            return sounds;
        }

        public static async Task<Sound> FetchSoundById(int id)
        {
            var data = await Helper.HttpClient.GetStringAsync($"https://api.meowpad.me/v1/sounds/{id}");

            var sound = SoundMeta.FromJson(data);

            return sound.Sound;
        }

        public static async Task<bool> DownloadSound(string name, string libraryPath)
        {
            var path = Path.Combine(libraryPath, name) + ".mp3";
            await using var f = File.OpenWrite(path);

            if (File.Exists(path)) return false;

            var stream = await Helper.HttpClient.GetStreamAsync($"https://api.meowpad.me/v1/download/{name}");
            await stream.CopyToAsync(f);


            return true;
        }
    }
}
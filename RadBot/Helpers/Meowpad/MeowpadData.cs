//    using RadBot.Helpers.Meowpad;
//
//    var meowpadData = MeowpadData.FromJson(jsonString);

#region

using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#endregion

namespace RadBot.Modules.Helpers.Meowpad
{
    public partial class MeowpadData
    {
        [JsonProperty("sounds")] public Sound[] Sounds { get; set; }

        [JsonProperty("meta")] public Meta Meta { get; set; }
    }

    public class Meta
    {
        [JsonProperty("hasNextPage")] public bool HasNextPage { get; set; }

        [JsonProperty("hasPreviousPage")] public bool HasPreviousPage { get; set; }

        [JsonProperty("pageIndex")] public long PageIndex { get; set; }

        [JsonProperty("totalPages")] public long TotalPages { get; set; }

        [JsonProperty("totalResults")] public long TotalResults { get; set; }

        [JsonProperty("aggregations")] public Aggregation[] Aggregations { get; set; }
    }

    public class Aggregation
    {
        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("items")] public Item[] Items { get; set; }
    }

    public class Item
    {
        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("count")] public long Count { get; set; }
    }

    public class Sound
    {
        [JsonProperty("soundTags")] public SoundTag[] SoundTags { get; set; }

        [JsonProperty("soundDownloads")] public object[] SoundDownloads { get; set; }

        [JsonProperty("soundListSounds")] public object[] SoundListSounds { get; set; }

        [JsonProperty("id")] public long Id { get; set; }

        [JsonProperty("soundId")] public Guid SoundId { get; set; }

        [JsonProperty("title")] public string Title { get; set; }

        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public string User { get; set; }

        [JsonProperty("region", NullValueHandling = NullValueHandling.Ignore)]
        public string Region { get; set; }

        [JsonProperty("created")] public DateTimeOffset Created { get; set; }

        [JsonProperty("fileLength")] public long FileLength { get; set; }

        [JsonProperty("duration")] public long Duration { get; set; }

        [JsonProperty("contentType")] public ContentType ContentType { get; set; }

        [JsonProperty("checksum")] public string Checksum { get; set; }

        [JsonProperty("downloads")] public long Downloads { get; set; }

        [JsonProperty("slug")] public string Slug { get; set; }

        [JsonProperty("extension")] public Extension Extension { get; set; }

        [JsonProperty("voting")] public long Voting { get; set; }

        [JsonProperty("screamDetection")] public ScreamDetection ScreamDetection { get; set; }
    }

    public class SoundTag
    {
        [JsonProperty("id")] public long Id { get; set; }

        [JsonProperty("soundTagId")] public Guid SoundTagId { get; set; }

        [JsonProperty("tag")] public Tag Tag { get; set; }

        [JsonProperty("voting")] public long Voting { get; set; }
    }

    public class Tag
    {
        [JsonProperty("id")] public long Id { get; set; }

        [JsonProperty("tagId")] public Guid TagId { get; set; }

        [JsonProperty("text")] public string Text { get; set; }
    }

    public enum ContentType
    {
        AudioMp3,
        AudioMpeg,
        AudioM4a
    }

    public enum Extension
    {
        Mp3,
        M4a
    }

    public enum ScreamDetection
    {
        Loud,
        None,
        Scream,
        DefinitelyScream
    }

    public partial class MeowpadData
    {
        public static MeowpadData FromJson(string json)
        {
            return JsonConvert.DeserializeObject<MeowpadData>(json, Converter.Settings);
        }
    }

    public static class Serialize
    {
        public static string ToJson(this MeowpadData self)
        {
            return JsonConvert.SerializeObject(self, Converter.Settings);
        }
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                ContentTypeConverter.Singleton,
                ExtensionConverter.Singleton,
                ScreamDetectionConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ContentTypeConverter : JsonConverter
    {
        public static readonly ContentTypeConverter Singleton = new ContentTypeConverter();

        public override bool CanConvert(Type t)
        {
            return t == typeof(ContentType) || t == typeof(ContentType?);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "audio/mp3":
                    return ContentType.AudioMp3;
                case "audio/mpeg":
                    return ContentType.AudioMpeg;
                case "audio/x-m4a":
                    return ContentType.AudioM4a;
            }

            throw new Exception("Cannot unmarshal type ContentType");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            var value = (ContentType)untypedValue;
            switch (value)
            {
                case ContentType.AudioMp3:
                    serializer.Serialize(writer, "audio/mp3");
                    return;
                case ContentType.AudioMpeg:
                    serializer.Serialize(writer, "audio/mpeg");
                    return;
            }

            throw new Exception("Cannot marshal type ContentType");
        }
    }

    internal class ExtensionConverter : JsonConverter
    {
        public static readonly ExtensionConverter Singleton = new ExtensionConverter();

        public override bool CanConvert(Type t)
        {
            return t == typeof(Extension) || t == typeof(Extension?);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case ".mp3":
                    return Extension.Mp3;
                case ".m4a":
                    return Extension.M4a;
            }

            throw new Exception("Cannot unmarshal type Extension");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            var value = (Extension)untypedValue;
            if (value == Extension.Mp3)
            {
                serializer.Serialize(writer, ".mp3");
                return;
            }

            throw new Exception("Cannot marshal type Extension");
        }
    }

    internal class ScreamDetectionConverter : JsonConverter
    {
        public static readonly ScreamDetectionConverter Singleton = new ScreamDetectionConverter();

        public override bool CanConvert(Type t)
        {
            return t == typeof(ScreamDetection) || t == typeof(ScreamDetection?);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "Loud":
                    return ScreamDetection.Loud;
                case "None":
                    return ScreamDetection.None;
                case "Scream":
                    return ScreamDetection.Scream;
                case "DefinitelyScream":
                    return ScreamDetection.DefinitelyScream;
            }

            throw new Exception("Cannot unmarshal type ScreamDetection");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            var value = (ScreamDetection)untypedValue;
            switch (value)
            {
                case ScreamDetection.Loud:
                    serializer.Serialize(writer, "Loud");
                    return;
                case ScreamDetection.None:
                    serializer.Serialize(writer, "None");
                    return;
                case ScreamDetection.Scream:
                    serializer.Serialize(writer, "Scream");
                    return;
            }

            throw new Exception("Cannot marshal type ScreamDetection");
        }
    }

    public partial class SoundMeta
    {
        [JsonProperty("sound")] public Sound Sound { get; set; }

        [JsonProperty("soundTags")] public object[] SoundTags { get; set; }

        [JsonProperty("tags")] public object[] Tags { get; set; }
    }

    public partial class SoundMeta
    {
        public static SoundMeta FromJson(string json)
        {
            return JsonConvert.DeserializeObject<SoundMeta>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
                Converters =
                {
                    new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal },
                    ContentTypeConverter.Singleton,
                    ExtensionConverter.Singleton,
                    ScreamDetectionConverter.Singleton
                },
            });
        }
    }
}
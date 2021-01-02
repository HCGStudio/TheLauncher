using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static HCGStudio.TheLauncherLib.Tools.HttpSingleton;

namespace HCGStudio.TheLauncherLib.GameFile
{
    public class MinecraftVersionMeta
    {
        [JsonProperty("latest")] public MinecraftLatest? Latest { get; set; }

        [JsonProperty("versions")] public List<MinecraftVersionInformation> Versions { get; set; } = new();

        public static async Task<MinecraftVersionMeta> GetMetaAsync()
        {
            var meta = JsonConvert.DeserializeObject<MinecraftVersionMeta>(
                await Http.GetStringAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json"));
            meta.Versions.Sort();
            return meta;
        }

        public class MinecraftLatest
        {
            public string? Release { get; set; }
            public string? Snapshot { get; set; }
        }

        public class MinecraftVersionInformation : IComparable<MinecraftVersionInformation>
        {
            [JsonProperty("id")] public string? Id { get; set; }

            [JsonProperty("type")] public string? Type { get; set; }

            [JsonProperty("url")] public string? Url { get; set; }

            [JsonProperty("time")] public DateTime Time { get; set; }

            [JsonProperty("releaseTime")] public DateTime ReleaseTime { get; set; }

            public int CompareTo(MinecraftVersionInformation? other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (ReferenceEquals(null, other)) return 1;
                return string.Compare(Id, other.Id, StringComparison.Ordinal);
            }
        }
    }
}
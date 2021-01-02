using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HCGStudio.TheLauncherLib.GameFile
{
    public class MinecraftAsset
    {
        [JsonProperty("hash")] public string Hash { get; set; } = string.Empty;

        [JsonProperty("size")] public int Size { get; set; }

        [JsonIgnore] public string Location => Path.Combine(Hash.Substring(0, 2), Hash);
    }

    public record MinecraftAssetDownload
    {
        public string Hash { get; init; } = string.Empty;
        public string DownloadUrl { get; init; } = string.Empty;
        public string SaveLocation { get; init; } = string.Empty;
    }


    public class MinecraftAssetCollection
    {
        [JsonProperty("objects")] public Dictionary<string, MinecraftAsset> Assets { get; set; } = new();

        private string BytesToString(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.AppendFormat(b.ToString("x2"));
            return sb.ToString();
        }

        private async Task<bool> ExistAndHashMatch(string fileName, string compare)
        {
            if (!File.Exists(fileName))
                return false;
            if (string.IsNullOrEmpty(compare))
                return true;

            using var sha = SHA1.Create();
            var hash = await sha.ComputeHashAsync(File.OpenRead(fileName));
            var hashString = BytesToString(hash);
            return hashString.Equals(compare, StringComparison.CurrentCultureIgnoreCase);
        }


        public async IAsyncEnumerable<MinecraftAssetDownload> EnsureAssets(string assetDir)
        {
            foreach (var (_, asset) in Assets)
            {
                var path = Path.Combine(assetDir, "objects", asset.Location);
                if (!await ExistAndHashMatch(path, asset.Hash))
                    yield return new()
                    {
                        Hash = asset.Hash,
                        DownloadUrl =
                            "http://resources.download.minecraft.net/" +
                            $"{asset.Hash.Substring(0, 2)}/{asset.Hash}",
                        SaveLocation = path
                    };
            }
        }
    }
}
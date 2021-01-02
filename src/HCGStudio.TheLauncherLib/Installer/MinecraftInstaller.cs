using System;
using System.IO;
using System.Threading.Tasks;
using HCGStudio.TheLauncherLib.Download;
using HCGStudio.TheLauncherLib.GameFile;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static HCGStudio.TheLauncherLib.Tools.HttpSingleton;

namespace HCGStudio.TheLauncherLib.Installer
{
    public class MinecraftInstaller : IInstaller
    {
        public async ValueTask InstallAsync(string gameVersion, string libDir, string assetDir, string profileDir)
        {
            Directory.CreateDirectory(profileDir);
            Directory.CreateDirectory(Path.Combine(assetDir, "indexes"));
            var meta = await MinecraftVersionMeta.GetMetaAsync();
            var index = meta.Versions.BinarySearch(new() {Id = gameVersion});
            if (index < 0)
                throw new ArgumentException();
            var required = meta.Versions[index];
            var minecraftJsonValue = await Http.GetStringAsync(required.Url);
            var minecraft = await MinecraftGame.ParseAsync(JObject.Parse(minecraftJsonValue),
                Array.Empty<string>(), profileDir);
            await File.WriteAllTextAsync(Path.Combine(profileDir, $"{gameVersion}.json"), minecraftJsonValue);
            var downloader = new FileDownloader();
            var libDownloadList = await minecraft.EnsureLibraryAsync(libDir);
            foreach (var lib in libDownloadList)
                downloader.Enqueue(lib.Url, Path.Combine(libDir, lib.Path), lib.Sha);

            var assetJsonString = await Http.GetStringAsync(minecraft.AssetUrl);
            await File.WriteAllTextAsync(Path.Combine(assetDir, "indexes", $"{minecraft.AssetIndex}.json"),
                assetJsonString);
            var assetList = JsonConvert.DeserializeObject<MinecraftAssetCollection>(assetJsonString);
            await foreach (var asset in assetList.EnsureAssets(assetDir))
                downloader.Enqueue(asset.DownloadUrl, asset.SaveLocation, asset.Hash);

            await downloader.Execute();
        }
    }
}
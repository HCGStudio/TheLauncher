using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using HCGStudio.TheLauncherLib.Download;
using HCGStudio.TheLauncherLib.GameFile;
using HCGStudio.TheLauncherLib.Login;

namespace HCGStudio.TheLauncherLib.StartUp
{
    public class Launcher
    {
        public Launcher(IAuthenticator authenticator, MinecraftGame game, MinecraftAssetCollection asset,
            string assetDir,
            string libDir, string startupDir, string javaPath)
        {
            Authenticator = authenticator;
            Game = game;
            Asset = asset;
            AssetDir = assetDir;
            LibDir = libDir;
            StartupDir = startupDir;
            JavaPath = javaPath;
            Options = new();
            ArgBuilder = new();
        }

        public IAuthenticator Authenticator { get; }
        public MinecraftGame Game { get; }
        public MinecraftAssetCollection Asset { get; }
        public string AssetDir { get; }
        public string LibDir { get; }
        public string StartupDir { get; }
        public string JavaPath { get; }
        public MinecraftArgBuilder ArgBuilder { get; }
        public Dictionary<string, string> Options { get; }

        public async ValueTask Run()
        {
            var downloader = new FileDownloader();
            var libDownloadList = await Game.EnsureLibraryAsync(LibDir);
            foreach (var lib in libDownloadList)
                downloader.Enqueue(lib.Url, Path.Combine(LibDir, lib.Path), lib.Sha);
            await foreach (var asset in Asset.EnsureAssets(AssetDir))
                downloader.Enqueue(asset.DownloadUrl, asset.SaveLocation, asset.Hash);
            await downloader.Execute();


            var nativePath = Path.Combine(StartupDir, "natives");
            var name = Assembly.GetExecutingAssembly().GetName();
            Options["version_name"] = Game.Id;
            Options["game_directory"] = StartupDir;
            Options["assets_root"] = AssetDir;
            Options["assets_index_name"] = Game.AssetIndex;
            Options["launcher_name"] = name.Name ?? "TheLauncher";
            Options["version_type"] = name.Name ?? "TheLauncher";
            Options["launcher_version"] = name.Version?.ToString() ?? "Unknown";
            await Game.ExtractNativeAsync(nativePath, LibDir);
            Options["natives_directory"] = $"{nativePath}";
            Options["classpath"] = Game.ProcessClassPath(LibDir);

            foreach (var (key, value) in await Authenticator.Authenticate())
                Options[key] = value;

            Game.ApplyArgs(Options);
            ArgBuilder.Append(Game);

            var info = new ProcessStartInfo(JavaPath)
            {
                WorkingDirectory = StartupDir
            };

            foreach (var s in ArgBuilder.Build())
                info.ArgumentList.Add(s);


            using var minecraft = Process.Start(info);
            if (minecraft != null)
                await minecraft.WaitForExitAsync();
        }
    }
}
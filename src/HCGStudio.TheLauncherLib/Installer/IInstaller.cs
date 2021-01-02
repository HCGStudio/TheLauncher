using System.Threading.Tasks;

namespace HCGStudio.TheLauncherLib.Installer
{
    public interface IInstaller
    {
        public ValueTask InstallAsync(string gameVersion, string libDir, string assetDir, string profileDir);
    }

    public interface IModLoaderInstaller : IInstaller
    {
        public ValueTask InstallAsync(string gameVersion, string loaderVersion, string libDir, string assetDir,
            string profileDir);
    }
}
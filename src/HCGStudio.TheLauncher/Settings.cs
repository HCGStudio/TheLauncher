using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCGStudio.TheLauncherLib.GameFile;
using HCGStudio.TheLauncherLib.Login;
using HCGStudio.TheLauncherLib.Tools;

namespace HCGStudio.TheLauncher
{
    public class MinecraftAccount
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Mojang";
        public Guid AccountGuid { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }

        public IAuthenticator GetAuthenticator(Guid clientToken)
        {
            return Type switch
            {
                "Mojang" => new OldAuthenticator(AccessToken ?? string.Empty, clientToken, AccountGuid, Name),
                "Microsoft" => new MicrosoftAuthenticator(RefreshToken ?? string.Empty),
                "Offline" => new OfflineAuthenticator() {Token = AccountGuid, UserName = Name},
                _ => throw new NotSupportedException()
            };
        }
    }

    public class MinecraftGameConfig
    {
        public string? CustomJava { get; set; }
        public string GameVersion { get; set; } = string.Empty;
        public string[] AdditionalArgs { get; set; } = Array.Empty<string>();
        public ulong Ram { get; set; } = 4096;

        public async Task<MinecraftGame> GetMinecraftGame(string versionDir, ICollection<string> features)
        {
            return await MinecraftGame.ParseProfileFile(Path.Combine(versionDir, GameVersion), versionDir, features);
        }
    }

    public class Settings
    {
        public Dictionary<string, MinecraftAccount> Accounts = new();
        public Dictionary<string, MinecraftGameConfig> Instances = new();
        public string DefaultJava { get; set; } = string.Empty;
        public string DefaultAccount { get; set; } = string.Empty;
        public Guid ClientToken { get; set; }
        public string DefaultGame { get; set; } = string.Empty;

        public static Settings Default()
        {
            var java = JavaDetector.EnumerateJavaPath().First();
            return new()
            {
                DefaultJava = java,
                ClientToken = Guid.NewGuid()
            };
        }
    }
}
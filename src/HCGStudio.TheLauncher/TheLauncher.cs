using System;
using System.IO;
using System.Threading.Tasks;
using HCGStudio.TheLauncherLib.GameFile;
using HCGStudio.TheLauncherLib.Login;
using HCGStudio.TheLauncherLib.StartUp;
using Newtonsoft.Json;
using PlasticMetal.MobileSuit;
using PlasticMetal.MobileSuit.ObjectModel;

namespace HCGStudio.TheLauncher
{
    public class TheLauncher : SuitClient
    {
        public TheLauncher(Settings config)
        {
            Config = config;
            Version = Directory.CreateDirectory("versions").FullName;
            Asset = Directory.CreateDirectory("assets").FullName;
            Lib = Directory.CreateDirectory("libraries").FullName;
            Games = Directory.CreateDirectory("games").FullName;
        }

        [SuitIgnore] public Settings Config { get; set; }

        [SuitIgnore] public string Version { get; }

        [SuitIgnore] public string Asset { get; }

        [SuitIgnore] public string Lib { get; }

        [SuitIgnore] public string Games { get; }

        private static string GetStringNotEmpty(string name)
        {
            var str = ReadLine.Read($"Input {name}:");
            while (string.IsNullOrWhiteSpace(str)) str = ReadLine.Read($"Input {name}:");

            return str;
        }

        private static string GetPasswordNotEmpty()
        {
            var password = ReadLine.ReadPassword("Your password:");
            while (string.IsNullOrWhiteSpace(password)) password = ReadLine.ReadPassword("Your password:");

            return password;
        }

        private async ValueTask<string> MicrosoftLogin()
        {
            var auth = new MicrosoftAuthenticator();
            var result = await auth.Authenticate();
            var name = result["auth_player_name"];
            var guid = Guid.Parse(result["auth_uuid"]);
            var accessToken = result["auth_access_token"];
            Config.Accounts[name] = new()
            {
                Name = name,
                AccessToken = accessToken,
                AccountGuid = guid,
                Type = "Microsoft",
                RefreshToken = auth.RefreshToken
            };
            if (string.IsNullOrEmpty(Config.DefaultAccount))
                Config.DefaultAccount = name;
            return name;
        }

        private async ValueTask<string> MojangLogin()
        {
            var userName = GetStringNotEmpty("user name");
            var password = GetPasswordNotEmpty();
            var auth = new OldAuthenticator(userName, password, Config.ClientToken);
            var result = await auth.Authenticate();
            var name = result["auth_player_name"];
            var guid = Guid.Parse(result["auth_uuid"]);
            var accessToken = result["auth_access_token"];
            Config.Accounts[name] = new()
            {
                Name = name,
                AccessToken = accessToken,
                AccountGuid = guid,
                Type = "Mojang"
            };
            if (string.IsNullOrEmpty(Config.DefaultAccount))
                Config.DefaultAccount = name;
            return name;
        }


        public async Task AddMojangAccount()
        {
            await MojangLogin();
            Console.WriteLine("Login success!");
        }

        public async Task AddMicrosoftAccount()
        {
            await MicrosoftLogin();
            Console.WriteLine("Login success!");
        }

        public void AddOfflineAccount(string userName)
        {
            Config.Accounts.Add(userName, new()
            {
                Name = userName,
                AccountGuid = Guid.NewGuid(),
                Type = "Offline"
            });
        }

        public void AddInstance(string gameVersion, string instanceName)
        {
            if (!File.Exists(Path.Combine(Version, $"{gameVersion}.json")))
            {
                Console.WriteLine("Base not found.");
                Console.WriteLine("To list all the base, use ListBase.");
                return;
            }

            Directory.CreateDirectory(Path.Combine(Games, instanceName));
            Config.Instances[instanceName] = new()
            {
                GameVersion = gameVersion
            };
        }

        public async Task Run(string accountName, string instanceName)
        {
            var account = Config.Accounts[accountName];
            var instance = Config.Instances[instanceName];
            var game = await instance.GetMinecraftGame(Version, Array.Empty<string>());
            var assetList = JsonConvert.DeserializeObject<MinecraftAssetCollection>(
                await File.ReadAllTextAsync(Path.Combine(Asset, "indexes", $"{game.AssetIndex}.json")));
            var launcher = new Launcher(account.GetAuthenticator(Config.ClientToken), game, assetList, Asset, Lib,
                Path.Combine(Games, instanceName), instance.CustomJava ?? Config.DefaultJava);
            await launcher.Run();
        }
    }
}
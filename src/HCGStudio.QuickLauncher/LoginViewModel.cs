using System;
using System.IO;
using System.Reactive;
using System.Windows;
using System.Windows.Forms;
using HCGStudio.TheLauncher;
using HCGStudio.TheLauncherLib.GameFile;
using HCGStudio.TheLauncherLib.Login;
using HCGStudio.TheLauncherLib.StartUp;
using Newtonsoft.Json;
using ReactiveUI;
using MessageBox = System.Windows.Forms.MessageBox;

namespace HCGStudio.QuickLauncher
{
    public class LoginViewModel : ReactiveObject
    {
        private string _password = string.Empty;
        private string _userName = string.Empty;

        private Visibility _visibility = Visibility.Visible;

        public LoginViewModel()
        {
            WhenLogin = ReactiveCommand.CreateFromTask(async v =>
            {
                Visibility = Visibility.Hidden;
                var auth = new OldAuthenticator(UserName, Password, App.Config.ClientToken);
                try
                {
                    var result = await auth.Authenticate();
                    var account = new MinecraftAccount
                    {
                        Name = result["auth_player_name"],
                        AccessToken = result["auth_access_token"],
                        AccountGuid = Guid.Parse(result["auth_uuid"]),
                        Type = result["user_type"]
                    };
                    App.Config.Accounts[account.Name] = account;
                    App.Config.DefaultAccount = account.Name;
                    App.Config = App.Config;
                    var version = Directory.CreateDirectory("versions").FullName;
                    var asset = Directory.CreateDirectory("assets").FullName;
                    var lib = Directory.CreateDirectory("libraries").FullName;
                    var games = Directory.CreateDirectory("games").FullName;
                    //Start game
                    var defaultGame = App.Config.Instances[App.Config.DefaultGame];
                    var game = await defaultGame
                        .GetMinecraftGame(version, Array.Empty<string>());
                    var assetList = JsonConvert.DeserializeObject<MinecraftAssetCollection>(
                        await File.ReadAllTextAsync(Path.Combine(asset, "indexes", $"{game.AssetIndex}.json")));
                    var launcher = new Launcher(auth, game, assetList, asset, lib,
                        Path.Combine(games, App.Config.DefaultGame), defaultGame.CustomJava ?? App.Config.DefaultJava);
                    await launcher.Run();
                    Environment.Exit(0);
                }
                catch (AuthenticationException e)
                {
                    MessageBox.Show(e.Message, "错误", MessageBoxButtons.OKCancel);
                    Visibility = Visibility.Visible;
                }
            });
        }

        public string UserName
        {
            get => _userName;
            set => this.RaiseAndSetIfChanged(ref _userName, value);
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public Visibility Visibility
        {
            get => _visibility;
            set => this.RaiseAndSetIfChanged(ref _visibility, value);
        }

        public ReactiveCommand<Unit, Unit> WhenLogin { get; }
    }
}
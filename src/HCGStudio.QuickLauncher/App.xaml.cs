using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using HCGStudio.TheLauncher;
using HCGStudio.TheLauncherLib.GameFile;
using HCGStudio.TheLauncherLib.Login;
using HCGStudio.TheLauncherLib.StartUp;
using HCGStudio.TheLauncherLib.Tools;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Application = System.Windows.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace HCGStudio.QuickLauncher
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Settings _config = new();

        public static Settings Config
        {
            get => _config;
            set
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                File.WriteAllText("config.yml", serializer.Serialize(_config));
                _config = value;
            }
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            new EmptyWindow().Show();
            await RunApp();
        }

        private async Task RunApp()
        {
            if (!File.Exists("config.yml"))
            {
                MessageBox.Show("找不到配置文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(255);
            }

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                _config = deserializer.Deserialize<Settings>(await File.ReadAllTextAsync("config.yml"));
                if (string.IsNullOrWhiteSpace(_config.DefaultGame))
                    throw new("需要默认游戏设置！");
                if (string.IsNullOrEmpty(_config.DefaultJava))
                {
                    _config.DefaultJava = JavaDetector.EnumerateJavaPath().FirstOrDefault() ?? string.Empty;
                    if (string.IsNullOrEmpty(_config.DefaultJava))
                    {
                        if (MessageBox.Show("您没有安装Java，是否下载？", "未检测到Java", MessageBoxButtons.YesNo) == DialogResult.OK)
                            Process.Start(
                                new ProcessStartInfo(
                                        "https://mirrors.tuna.tsinghua.edu.cn/AdoptOpenJDK/8/jre/x64/windows/")
                                    {UseShellExecute = true});
                        Environment.Exit(255);
                    }

                    Config = _config;
                }

                if (_config.ClientToken == Guid.Empty)
                {
                    _config.ClientToken = Guid.NewGuid();
                    Config = _config;
                }

                //Login required
                if (string.IsNullOrEmpty(_config.DefaultAccount))
                {
                    new LoginWindow().Show();
                    return;
                }

                var defaultAccount = _config.Accounts[_config.DefaultAccount];
                var defaultGame = _config.Instances[_config.DefaultGame];
                var auth = defaultAccount.GetAuthenticator(_config.ClientToken);
                try
                {
                    var authResult = await auth.Authenticate();
                    defaultAccount.Name = authResult["auth_player_name"];
                    defaultAccount.AccessToken = authResult["auth_access_token"];
                    defaultAccount.AccountGuid = Guid.Parse(authResult["auth_uuid"]);
                    Config = _config;
                    var version = Directory.CreateDirectory("versions").FullName;
                    var asset = Directory.CreateDirectory("assets").FullName;
                    var lib = Directory.CreateDirectory("libraries").FullName;
                    var games = Directory.CreateDirectory("games").FullName;
                    //Start game
                    var game = defaultGame.GetMinecraftGame(version, Array.Empty<string>()).Result;
                    var assetList = JsonConvert.DeserializeObject<MinecraftAssetCollection>(
                        await File.ReadAllTextAsync(Path.Combine(asset, "indexes", $"{game.AssetIndex}.json")));
                    var launcher = new Launcher(auth, game, assetList, asset, lib,
                        Path.Combine(games, _config.DefaultGame), defaultGame.CustomJava ?? Config.DefaultJava);
                    await launcher.Run();
                }
                catch (AuthenticationException)
                {
                    //Re-Enter password required
                    new LoginWindow().Show();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(255);
            }
        }
    }
}
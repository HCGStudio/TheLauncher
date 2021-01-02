using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using HCGStudio.TheLauncherLib.Installer;
using PlasticMetal.MobileSuit;
using PlasticMetal.MobileSuit.Core;
using PlasticMetal.MobileSuit.ObjectModel.Future;
using PlasticMetal.MobileSuit.ObjectModel.Premium;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace HCGStudio.TheLauncher
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var config = !File.Exists("config.yml")
                ? Settings.Default()
                : deserializer.Deserialize<Settings>(await File.ReadAllTextAsync("config.yml"));

            try
            {
                await Parser.Default.ParseArguments<Options>(args)
                    .WithParsedAsync(async o =>
                    {
                        if (o.Install != null)
                        {
                            var version = Directory.CreateDirectory("versions").FullName;
                            var asset = Directory.CreateDirectory("assets").FullName;
                            var lib = Directory.CreateDirectory("libraries").FullName;
                            var games = Directory.CreateDirectory("games").FullName;
                            //Install minecraft
                            Console.WriteLine("Begin downloading files.");
                            var installer = new MinecraftInstaller();

                            await installer.InstallAsync(o.Install, lib, asset, version);
                            if (!config.Instances.ContainsKey(o.Install))
                                config.Instances.Add(o.Install, new() {GameVersion = o.Install});

                            Directory.CreateDirectory(Path.Combine(games, o.Install));
                            Console.WriteLine("Done");
                            return;
                        }

                        var client = new TheLauncher(config);

                        Suit.GetBuilder()
                            .UseLog(ILogger.OfDirectory(Directory.CreateDirectory("logs").FullName))
                            .UsePrompt<PowerLineThemedPromptServer>()
                            .UseBuildInCommand<DiagnosticBuildInCommandServer>()
                            .Build(client).Run();
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                await File.WriteAllTextAsync("config.yml", serializer.Serialize(config));
            }
        }

        public class Options
        {
            [Option('i', "install")] public string? Install { get; set; }

            [Option("fabric")] public string? Fabric { get; set; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using MavenSharp;
using Newtonsoft.Json.Linq;
using static HCGStudio.TheLauncherLib.Tools.HttpSingleton;

namespace HCGStudio.TheLauncherLib.GameFile
{
    public class MinecraftGame : IArgProvider
    {
        public string[] GameArguments { get; set; } = Array.Empty<string>();
        public string[] JvmArguments { get; set; } = Array.Empty<string>();
        public MinecraftLib[] Libraries { get; set; } = Array.Empty<MinecraftLib>();
        public MinecraftLib[] NativeLibraries { get; set; } = Array.Empty<MinecraftLib>();
        public MinecraftAsset? Assets { get; set; }
        public string AssetIndex { get; set; } = string.Empty;
        public string AssetUrl { get; set; } = string.Empty;
        public string AssetSha { get; set; } = string.Empty;
        public string ClientUrl { get; set; } = string.Empty;
        public string ClientSha { get; set; } = string.Empty;
        public string PossibleMinecraftVersion { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string ClientPath => Path.Combine("net", "minecraft", "client", Id, $"{Id}.jar");
        public string MainClass { get; set; } = string.Empty;

        public IEnumerable<string> EnumerateArg()
        {
            foreach (var jvmArgument in JvmArguments) yield return jvmArgument;
            yield return MainClass;
            foreach (var gameArgument in GameArguments) yield return gameArgument;
        }

        public string ProcessClassPath(string baseDir)
        {
            if (!Directory.Exists(baseDir))
                throw new DirectoryNotFoundException();
            var classPathBuilder = new StringBuilder();
            foreach (var minecraftLib in Libraries)
                classPathBuilder
                    .Append(Path.Combine(baseDir, minecraftLib.Path))
                    .Append(';');
            foreach (var minecraftLib in NativeLibraries)
                classPathBuilder
                    .Append(Path.Combine(baseDir, minecraftLib.Path))
                    .Append(';');

            classPathBuilder.Append(Path.Combine(baseDir, ClientPath));

            return classPathBuilder.ToString();
        }

        public async ValueTask ExtractNativeAsync(string nativeDir, string libDir)
        {
            var dynamicLibSuffix = ".so";
            if (OperatingSystem.IsWindows())
                dynamicLibSuffix = ".dll";
            if (OperatingSystem.IsMacOS())
                dynamicLibSuffix = ".dylib";
            Directory.CreateDirectory(nativeDir);
            foreach (var lib in NativeLibraries)
            {
                var nativeFile = Path.Combine(libDir, lib.Path);
                if (!File.Exists(nativeFile))
                    continue;
                var zip = new ZipFile(nativeFile);
                foreach (ZipEntry entry in zip)
                {
                    if (!entry.IsFile || !entry.Name.EndsWith(dynamicLibSuffix))
                        continue;
                    var dest = Path.Combine(nativeDir, entry.Name);
                    await using var stream = zip.GetInputStream(entry);
                    await stream.CopyToAsync(File.OpenWrite(dest));
                }
            }
        }

        private string BytesToString(byte[] bytes)
        {
            var sb = new StringBuilder();

            foreach (var b in bytes) sb.AppendFormat(b.ToString("x2"));

            return sb.ToString();
        }

        private async IAsyncEnumerable<MinecraftLib> EnsureCommonLibraryAsync(string baseDir)
        {
            using var sha = SHA1.Create();
            foreach (var lib in Libraries)
            {
                var path = Path.Combine(baseDir, lib.Path);
                if (await EnsureExistAndHash(path, lib.Sha, sha))
                    continue;
                yield return lib;
            }
        }

        private async IAsyncEnumerable<MinecraftLib> EnsureNativeLibraryAsync(string baseDir)
        {
            using var sha = SHA1.Create();
            foreach (var lib in NativeLibraries)
            {
                var path = Path.Combine(baseDir, lib.Path);
                if (await EnsureExistAndHash(path, lib.Sha, sha))
                    continue;
                yield return lib;
            }
        }

        private async Task<bool> EnsureExistAndHash(string file, string requiredHash, HashAlgorithm sha)
        {
            if (!File.Exists(file)) return false;
            if (string.IsNullOrEmpty(requiredHash))
                return true;
            await using var stream = File.OpenRead(file);
            var hash = await sha.ComputeHashAsync(stream);
            var hashString = BytesToString(hash);
            return hashString.Equals(requiredHash, StringComparison.CurrentCultureIgnoreCase);
        }

        public async Task<List<MinecraftLib>> EnsureLibraryAsync(string baseDir)
        {
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);
            var filesToDownload = new List<MinecraftLib>();

            //Ensure client
            var clientFullPath = Path.Combine(baseDir, ClientPath);
            if (!await EnsureExistAndHash(clientFullPath, ClientSha, SHA1.Create()))
                filesToDownload.Add(new()
                {
                    Url = ClientUrl,
                    Sha = ClientSha,
                    Path = ClientPath
                });

            await foreach (var lib in EnsureCommonLibraryAsync(baseDir))
                filesToDownload.Add(lib);

            await foreach (var lib in EnsureNativeLibraryAsync(baseDir))
                filesToDownload.Add(lib);

            return filesToDownload;
        }

        public void ApplyArgs(IDictionary<string, string> options)
        {
            ProcessArgString(GameArguments, options);
            ProcessArgString(JvmArguments, options);
        }

        private void ProcessArgString(IList<string> origin, IDictionary<string, string> options)
        {
            for (var i = 0; i < origin.Count; ++i)
            {
                var match = Regex.Match(origin[i], @"(?<=\$\{).*?(?=\})");
                if (!match.Success)
                    continue;
                var content = match.Result("$0");
                if (string.IsNullOrEmpty(content))
                    continue;
                if (options.TryGetValue(content, out var result))
                    origin[i] = origin[i].Replace($"${{{content}}}", result);
            }
        }

#nullable disable
        private static IEnumerable<string> GetStringFromArrayOrValue(JToken val)
        {
            switch (val.Type)
            {
                case JTokenType.Array:
                {
                    foreach (var arg in val)
                        yield return (string) arg;
                    break;
                }
                case JTokenType.String:
                    yield return (string) val;
                    break;
            }
        }


        public static async Task<MinecraftGame> ParseProfileFile(string profileName, string baseDir,
            ICollection<string> features)
        {
            return await ParseAsync(
                JObject.Parse(await File.ReadAllTextAsync(Path.Combine(baseDir, $"{profileName}.json"))), features,
                baseDir);
        }

        internal static async Task<MinecraftGame> ParseAsync(JObject source, ICollection<string> features,
            string baseDir)
        {
            var minecraft = new MinecraftGame();
            var gameArgs = new List<string>();
            var jvmArgs = new List<string>();
            var libraries = new List<MinecraftLib>();
            var natives = new List<MinecraftLib>();

            //Process inheritsFrom
            var inheritsFromToken = source.SelectToken("inheritsFrom");
            if (inheritsFromToken != null)
            {
                var versionFile = Path.Combine(baseDir, $"{inheritsFromToken}.json");
                if (!File.Exists(versionFile))
                {
                    //Manually download required minecraft
                    var meta = await MinecraftVersionMeta.GetMetaAsync();
                    var required = meta.Versions.BinarySearch(new() {Id = (string) inheritsFromToken});
                    if (required > 0)
                    {
                        var requireVersion = meta.Versions[required];
                        var value = await Http.GetStringAsync(requireVersion.Url);
                        minecraft = await ParseAsync(JObject.Parse(value), features, baseDir);
                        await File.WriteAllTextAsync(versionFile, value);
                        gameArgs.AddRange(minecraft.GameArguments);
                        jvmArgs.AddRange(minecraft.JvmArguments);
                        libraries.AddRange(minecraft.Libraries);
                        natives.AddRange(minecraft.NativeLibraries);
                    }
                }
                else
                {
                    minecraft = await ParseProfileFile((string) inheritsFromToken, baseDir, features);
                    gameArgs.AddRange(minecraft.GameArguments);
                    jvmArgs.AddRange(minecraft.JvmArguments);
                    libraries.AddRange(minecraft.Libraries);
                    natives.AddRange(minecraft.NativeLibraries);
                }
            }


            //Process game arguments
            var gameArgumentToken = source.SelectToken("arguments.game");
            if (gameArgumentToken != null)
                foreach (var val in gameArgumentToken.Children())
                    if (val.Type == JTokenType.String)
                    {
                        gameArgs.Add((string) val);
                    }
                    else
                    {
                        var rule = val["rules"]?.Children().FirstOrDefault();
                        var ruleFeatures = rule?["features"];
                        if (ruleFeatures == null)
                            continue;
                        if (ruleFeatures.Children().All(s => features.Contains((s as JProperty)?.Name)))
                            gameArgs.AddRange(GetStringFromArrayOrValue(val["value"]));
                    }


            //Process JVM arguments
            var jvmArgumentToken = source.SelectToken("arguments.jvm");
            if (jvmArgumentToken != null)
                foreach (var val in jvmArgumentToken)
                    if (val is JValue s)
                    {
                        jvmArgs.Add(s.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        var rules = val["rules"]?.Children().Select(rule =>
                            (MinecraftRule) new()
                            {
                                Allow = rule["action"]?.ToString() == "allow",
                                System = rule["os"]?["name"]?.ToString() ?? ""
                            }).ToList();

                        if (rules?.IsAllowed() == true) jvmArgs.AddRange(GetStringFromArrayOrValue(val["value"]));
                    }


            //Asset
            minecraft.AssetIndex = (string) source.SelectToken("assetIndex.id") ?? minecraft.AssetIndex;
            minecraft.AssetSha = (string) source.SelectToken("assetIndex.sha1") ?? minecraft.AssetSha;
            minecraft.AssetUrl = (string) source.SelectToken("assetIndex.url") ?? minecraft.AssetUrl;

            minecraft.ClientSha = (string) source.SelectToken("downloads.client.sha1") ?? minecraft.ClientSha;
            minecraft.ClientUrl = (string) source.SelectToken("downloads.client.url") ?? minecraft.ClientUrl;
            minecraft.Id = (string) source.SelectToken("id") ?? minecraft.Id;

            if (string.IsNullOrEmpty(minecraft.PossibleMinecraftVersion))
                minecraft.PossibleMinecraftVersion = minecraft.Id;

            //libs

            var librariesToken = source.SelectToken("libraries");
            if (librariesToken != null)
                foreach (var lib in librariesToken)
                {
                    if (lib["rules"] != null)
                    {
                        var rules = lib["rules"].Select(rule => (MinecraftRule) new()
                            {
                                Allow = rule["action"].ToString() == "allow",
                                System = rule["os"]?["name"]?.ToString() ?? ""
                            })
                            .ToList();
                        if (!rules.IsAllowed())
                            continue;
                    }

                    if (lib["downloads"]?["classifiers"] != null)
                    {
                        var nativeName = string.Empty;
                        if (OperatingSystem.IsWindows())
                            nativeName = (string) lib["natives"]?["windows"];
                        if (OperatingSystem.IsLinux())
                            nativeName = (string) lib["natives"]?["linux"];
                        if (OperatingSystem.IsMacOS())
                            nativeName = (string) lib["natives"]?["osx"];
                        if (nativeName == null)
                            continue;
                        var native = lib["downloads"]?["classifiers"][nativeName];
                        if (native != null)
                            natives.Add(new()
                            {
                                Path = (OperatingSystem.IsWindows()
                                    ? ((string) native["path"])?.Replace('/', '\\')
                                    : (string) native["path"]) ?? string.Empty,
                                Sha = (string) native["sha1"] ?? string.Empty,
                                Url = (string) native["url"] ?? string.Empty
                            });

                        continue;
                    }

                    var path = (string) lib["downloads"]?["artifact"]?["path"] ?? string.Empty;
                    var url = (string) lib["downloads"]?["artifact"]?["url"] ?? string.Empty;
                    var sha = (string) lib["downloads"]?["artifact"]?["sha1"] ?? string.Empty;
                    if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(url))
                    {
                        var mavenUrl = (string) lib["url"];
                        var name = (string) lib["name"];
                        if (string.IsNullOrEmpty(mavenUrl) || string.IsNullOrEmpty(name))
                            continue;
                        var repo = new MavenRepository(mavenUrl);
                        var packageUrl = repo.Get_Package_Url(new(name));
                        url = packageUrl.AbsoluteUri;
                        sha = await Http.GetStringAsync($"{url}.sha1");
                        path = string.Join("", packageUrl.Segments.Where(s => s != "/"));
                    }

                    libraries.Add(new()
                    {
                        Path = OperatingSystem.IsWindows() ? path.Replace('/', '\\') : path,
                        Sha = sha,
                        Url = url
                    });
                }

            minecraft.GameArguments = gameArgs.ToArray();
            minecraft.JvmArguments = jvmArgs.ToArray();
            minecraft.Libraries = libraries.ToArray();
            minecraft.NativeLibraries = natives.ToArray();
            minecraft.MainClass = (string) source["mainClass"] ?? string.Empty;
            return minecraft;
        }
#nullable restore
    }
}
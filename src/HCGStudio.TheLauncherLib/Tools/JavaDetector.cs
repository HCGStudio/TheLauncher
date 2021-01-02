using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HCGStudio.TheLauncherLib.Tools
{
    public static class JavaDetector
    {
        private static readonly string[] WindowsCommonInstallDir =
        {
            //AdoptOpenJdk
            Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles")
                         ?? @"C:\Program Files", "AdoptOpenJDK"),
            //Zulu
            Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles")
                         ?? @"C:\Program Files", "Zulu"),
            //Oracle
            Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles")
                         ?? @"C:\Program Files", "Java"),
            //Minecraft
            Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)")
                         ?? @"C:\Program Files (x86)", @"MinecraftGameArgProvider Launcher\runtime")
        };

        public static IEnumerable<string> EnumerateJavaPath()
        {
            var windows = OperatingSystem.IsWindows();
            var path = Environment.GetEnvironmentVariable("PATH")?
                .Split(windows ? ';' : ':');
            Debug.Assert(path != null, nameof(path) + " != null");

            //Find in path
            foreach (var p in path)
            {
                if (windows)
                {
                    var javaWindows = Path.Combine(p, "javaw.exe");
                    if (File.Exists(javaWindows))
                        yield return javaWindows;
                }

                var java = Path.Combine(p, windows ? "java.exe" : "java");
                if (!File.Exists(java)) continue;
                yield return java;
                break;
            }

            //Special check commonly install Location for windows
            if (windows)
                foreach (var dir in WindowsCommonInstallDir.Where(Directory.Exists))
                foreach (var p in Directory.EnumerateDirectories(dir))
                {
                    var javaWindows = Path.Combine(p, "bin\\javaw.exe");
                    if (File.Exists(javaWindows))
                        yield return javaWindows;
                    var java = Path.Combine(p, "bin\\java.exe");
                    if (File.Exists(java))
                        yield return java;
                }

            //Special check /usr/lib/jvm for linux
            if (OperatingSystem.IsLinux() && Directory.Exists("/usr/lib/jvm"))
                foreach (var jvm in Directory.EnumerateDirectories("/usr/lib/jvm"))
                {
                    var java = Path.Combine(jvm, "bin/java");
                    if (File.Exists(java))
                        yield return java;
                }

            //Special check /Library/Java/JavaVirtualMachines for macOS
            if (OperatingSystem.IsMacOS() && Directory.Exists("/Library/Java/JavaVirtualMachines"))
                foreach (var jvm in Directory.EnumerateDirectories("/Library/Java/JavaVirtualMachines"))
                {
                    var java = Path.Combine(jvm, "Contents/Home/bin/java");
                    if (File.Exists(java))
                        yield return java;
                }
        }

        public static async IAsyncEnumerable<JavaInfo> EnumerateJava()
        {
            var source = new CancellationTokenSource();
            var tasks = (from java in EnumerateJavaPath()
                let check = Process.Start(new ProcessStartInfo(java, "-version") {RedirectStandardError = true})
                select Task.Run(async () =>
                {
                    await check!.WaitForExitAsync(source.Token);
                    var sr = check.StandardError;

                    var returnValue = OperatingSystem.IsWindows()
                        ? (await sr.ReadToEndAsync()).Replace("\r", "")
                        : await sr.ReadToEndAsync();

                    sr.Dispose();
                    check.Dispose();
                    return (java, returnValue);
                }, source.Token)).ToList();

            source.CancelAfter(1000);
            await Task.WhenAll(tasks);

            foreach (var task in tasks.Where(task => !task.IsCanceled))
            {
                var (java, output) = task.Result;
                var val = output.Split('\n');
                if (val.Length < 3)
                    continue;
                var version = Regex.Match(val[0], "(?<=\").*?(?=\")").Result("$0");
                var distribution = Regex.Match(val[1], @"(?<=Environment ).*?(?=\(build)").Result("$0");
                yield return new()
                {
                    Location = java,
                    Distribution = distribution,
                    Version = version,
                    IsOpen = val[0].StartsWith("openjdk")
                };
            }
        }
    }
}
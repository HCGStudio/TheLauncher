using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static HCGStudio.TheLauncherLib.Tools.HttpSingleton;

namespace HCGStudio.TheLauncherLib.Download
{
    public class FileDownloader : IDisposable
    {
        public FileDownloader(int maxParallel = 32, int maxTry = 3)
        {
            MaxParallel = maxParallel;
            MaxTry = maxTry;
        }

        public int MaxParallel { get; }
        public int MaxTry { get; }
        private Queue<(string, string, string)> DownloadQueue { get; } = new();

        public void Dispose()
        {
            Http.Dispose();
        }

        private string BytesToString(byte[] bytes)
        {
            var sb = new StringBuilder();

            foreach (var b in bytes) sb.AppendFormat(b.ToString("x2"));

            return sb.ToString();
        }

        private bool CheckHashMatch(string compare, byte[] value)
        {
            if (string.IsNullOrEmpty(compare))
                return true;
            using var sha = SHA1.Create();
            var hash = sha.ComputeHash(value);
            var hashString = BytesToString(hash);

            return hashString.Equals(compare, StringComparison.CurrentCultureIgnoreCase);
        }

        public void Enqueue(string downloadUrl, string saveTo, string hash)
        {
            DownloadQueue.Enqueue((downloadUrl, saveTo, hash));
        }

        public async ValueTask Execute(CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(DownloadQueue,
                    new() {CancellationToken = cancellationToken, MaxDegreeOfParallelism = MaxParallel},
                    tuple =>
                    {
                        var (downloadUrl, saveTo, hash) = tuple;
                        try
                        {
                            Console.WriteLine($"Begin downloading {downloadUrl}.");
                            for (var i = 0; i < MaxTry; i++)
                            {
                                var response = Http.GetAsync(downloadUrl, cancellationToken).Result;
                                var downloaded = response.Content.ReadAsByteArrayAsync(cancellationToken).Result;
                                if (!CheckHashMatch(hash, downloaded))
                                    continue;
                                Directory.CreateDirectory(Path.GetDirectoryName(saveTo) ??
                                                          throw new InvalidOperationException());
                                File.WriteAllBytes(saveTo, downloaded);
                                Console.WriteLine($"{downloadUrl} saved to {saveTo}.");
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    });
            }, cancellationToken);
            DownloadQueue.Clear();
        }
    }
}
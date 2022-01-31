using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ReliableDownloader
{
    public class FileDownloader : IFileDownloader
    {
        public long bytesPerSecond { get; set; }
        ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
        CancellationTokenSource tokenSource = new CancellationTokenSource();

        private static IWebSystemCalls _systemCall;

        public FileDownloader()
        {
            _systemCall = new WebSystemCalls();
            bytesPerSecond = 500;
        }

        private async Task<long> GetFileLength(string contentFileUrl)
        {
            CancellationToken ct = tokenSource.Token;

            //1. Get total size of the file
            var lengthResult = await _systemCall.GetHeadersAsync(contentFileUrl, ct);
            var totalSize = lengthResult.Content.Headers.ContentLength ?? default(long);
            return totalSize;
        }

        public async Task<bool> DownloadFile(string contentFileUrl, string localFilePath, Action<FileProgress> onProgressChanged)
        {
            //1. Get File Size
            long totalSize = await GetFileLength(contentFileUrl);

            long timeForOnePacket = 4;
            //2. Determine the magic number for later
            long sizeOfPacket = bytesPerSecond * timeForOnePacket; //bytes

            //3. Divide the total size by some number
            int len = (int)(totalSize / sizeOfPacket) + 1; // 247 / 10 = 24
            //Console.WriteLine("Number of Packets: {0}", len);

            //3. Get part bytes
            FileStream fs = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
            BufferedStream st = new BufferedStream(fs);
            await st.WriteAsync(new byte[totalSize], 0, (int)totalSize);
            fs.Close();

            long downloadSize = 0;
            Task[] taskArray = new Task[len];
            TimeSpan span = new TimeSpan();

            Parallel.For(0, len, (index) =>
            //for (int index = 0; index < len; index++)
            {
                long from = index * sizeOfPacket;
                long to = Math.Min((index + 1) * sizeOfPacket - 1, totalSize - 1);
                //Console.WriteLine("{0}: {1} to {2}", index + 1, from, to);

                Task t = Task.Run(async () =>
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    await Task.Run(async () =>
                    {
                        return await _systemCall.DownloadPartialContent(contentFileUrl, from, to, tokenSource.Token);
                    })
                    .ContinueWith(async (fileDLTask) =>
                    {
                        var arrayRes = await fileDLTask.Result.Content.ReadAsByteArrayAsync();
                        sw.Stop();
                        Interlocked.Add(ref downloadSize, arrayRes.Length);

                        span += sw.Elapsed;
                        if (onProgressChanged != null)
                        {
                            var percentComplete = (double)downloadSize / totalSize;
                            FileProgress p = new FileProgress(totalSize, downloadSize, percentComplete*100, (1 - percentComplete) * span);
                            //Console.WriteLine("Download Size of {0} bytes from {1}; Remaining Time: {2}", downloadSize, from, ((1 - percentComplete) * span.Milliseconds));
                            onProgressChanged(p);
                        }

                        using (FileStream sourceStream = new FileStream(localFilePath,
                        FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write,
                        bufferSize: (int)(to - from + 1), useAsync: true))
                        {
                            sourceStream.Seek(from, SeekOrigin.Begin);
                            await sourceStream.WriteAsync(arrayRes, 0, arrayRes.Length);
                        };
                    })
                    .ContinueWith(handledResult =>
                    {
                    });
                });

                tasks.Add(t);
            }
            );
            Task.WaitAll(tasks.ToArray());

            //bool retValue = true;
            return true;
        }

        public void CancelDownloads()
        {
            tokenSource.Cancel();
        }
    }
}
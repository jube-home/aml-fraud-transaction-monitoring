/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Jube.Test.Load;

public class Load
{
    private readonly Stopwatch swTotal = new();
    private Thread? writerThreadRequests;
    private Thread? writerThreadTps;
    private readonly ConcurrentQueue<(double, long)> responseTimes = new();
    private int requests;
    private bool stop;

    [Theory]
    [InlineData("https://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc",
        10000, 1000000, false, 10, false, 10)]
    public Task LoadTest(string uriString, int httpTimeout, int iteration, bool async,
        int maxConnectionsPerServer, bool saturateMaxConnections, int timeDriftSeconds)
    {
        var random = new Random();
        var referenceDate = DateTime.Now.AddYears(-10);
        var uri = async ? new Uri(uriString + "/async") : new Uri(uriString);

        var stringTemplate = Helpers.ReadFileContents("Load/Mock.json");

        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.MaxConnectionsPerServer = maxConnectionsPerServer;
        httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        using var client = new HttpClient(httpClientHandler);
        client.Timeout = TimeSpan.FromMilliseconds(httpTimeout);

        var iterationCount = 0;
        var tasks = new List<Task>();

        writerThreadRequests = new Thread(WriterThreadWorker);
        writerThreadRequests.Start();

        writerThreadTps = new Thread(WriteTpsEstimates);
        writerThreadTps.Start();

        swTotal.Start();

        while (iterationCount <= iteration)
        {
            for (var j = 0;
                 j < (saturateMaxConnections
                     ? maxConnectionsPerServer
                     : random.NextInt64(0,
                         httpClientHandler.MaxConnectionsPerServer - tasks.Count(c => !c.IsCompleted)));
                 j++)
            {
                var stringReplaced = stringTemplate.Replace("[@AccountId@]", random.NextInt64(1, 100000).ToString());
                stringReplaced = stringReplaced.Replace("[@TxnId@]", iterationCount.ToString());

                referenceDate = referenceDate.AddSeconds(timeDriftSeconds);
                stringReplaced = stringReplaced.Replace("[@TxnDateTime@]", referenceDate.ToString("o"));

                tasks.Add(SendToJubeAndAwaitResponse(stringReplaced, uri, client, responseTimes, swTotal));

                requests += 1;
                iterationCount += 1;
            }

            Task.WaitAny(tasks.ToArray());
            tasks.RemoveAll(r => r.IsCompleted);
        }

        stop = true;
        
        return Task.CompletedTask;
    }

    private static async Task SendToJubeAndAwaitResponse(string stringReplaced, Uri uri, HttpClient client,
        ConcurrentQueue<(double, long)> responseTimes, Stopwatch swTotal)
    {
        var stringContent = new StringContent(
            stringReplaced,
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = uri,
            Content = stringContent
        };

        var sw = new Stopwatch();
        sw.Start();
        await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        sw.Stop();
        responseTimes.Enqueue((swTotal.Elapsed.TotalSeconds, sw.ElapsedMilliseconds));
    }

    private async void WriteTpsEstimates()
    {
        var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var outputFileTpsSnapshot = new StreamWriter(Path.Combine(docPath, "WriteLinesTpsSnapshot.txt"));
        outputFileTpsSnapshot.AutoFlush = true;
        
        while (!stop)
        {
            Thread.Sleep(1000);
            await outputFileTpsSnapshot.WriteLineAsync(
                $"{Math.Round(swTotal.Elapsed.TotalSeconds)},{requests}");
            requests = 0;
        }
    }

    private async void WriterThreadWorker(object? o)
    {
        var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var outputFileRequests = new StreamWriter(Path.Combine(docPath, "WriteLinesRequests.txt"));

        var flushInterval = 0;
        while (!stop)
        {
            while (responseTimes.TryDequeue(out var response))
            {
                await outputFileRequests.WriteLineAsync($"{response.Item1},{response.Item2}");

                if (flushInterval > 100)
                {
                    await outputFileRequests.FlushAsync();
                    flushInterval = 0;
                }
                else
                {
                    flushInterval += 1;
                }
            }

            Thread.Sleep(100);
        }

        await outputFileRequests.FlushAsync();
    }
}
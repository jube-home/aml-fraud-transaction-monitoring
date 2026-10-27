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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jube.Test.Case.Models;

public sealed class LoopbackListener : IDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly Task loop;
    private readonly ConcurrentQueue<string> paths = new();
    private readonly Func<string, (int Status, string? Location)> respond;

    public LoopbackListener(Func<string, (int Status, string? Location)>? respond = null)
    {
        this.respond = respond ?? (_ => (200, null));

        listener.Start();
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        loop = Task.Run(RunAsync);
    }

    public int Port { get; }

    public string[] Paths => [.. paths];

    private async Task RunAsync()
    {
        while (!stop.IsCancellationRequested)
        {
            TcpClient client;

            try
            {
                client = await listener.AcceptTcpClientAsync(stop.Token);
            }
            catch
            {
                return;
            }

            _ = Task.Run(() => ServeAsync(client));
        }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);

                var requestLine = await reader.ReadLineAsync();

                if (requestLine == null)
                {
                    return;
                }

                var contentLength = 0;
                string? header;

                while (!string.IsNullOrEmpty(header = await reader.ReadLineAsync()))
                {
                    if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    {
                        contentLength = int.Parse(header[15..].Trim());
                    }
                }

                var buffer = new char[contentLength];
                var read = 0;

                while (read < contentLength)
                {
                    var n = await reader.ReadAsync(buffer, read, contentLength - read);

                    if (n == 0)
                    {
                        break;
                    }

                    read += n;
                }

                var path = requestLine.Split(' ')[1];
                paths.Enqueue(path);

                var (status, location) = respond(path);
                var head = $"HTTP/1.1 {status} X\r\nContent-Length: 0\r\nConnection: close\r\n"
                           + (location != null ? $"Location: {location}\r\n" : string.Empty) + "\r\n";

                await stream.WriteAsync(Encoding.ASCII.GetBytes(head));
                await stream.FlushAsync();
            }
            catch (IOException)
            {
            }
        }
    }

    public void Dispose()
    {
        stop.Cancel();
        listener.Stop();

        try
        {
            loop.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }

        stop.Dispose();
    }
}

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

using Jube.Test.Infrastructure;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jube.Test.Mocks
{
    public sealed class MocksHost : IAsyncDisposable
    {
        private static readonly Regex guidPattern = new(
            "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.Compiled);

        private readonly WebApplication app;
        private readonly HttpClient client;
        private readonly Uri baseAddress;

        private MocksHost(WebApplication app)
        {
            this.app = app;
            baseAddress = new Uri(app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>().Required().Addresses.Single());
            client = new HttpClient { BaseAddress = baseAddress };
        }

        public async ValueTask DisposeAsync()
        {
            client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }

        public static async Task<MocksHost> StartAsync(Action<WebApplicationBuilder> configure,
            Action<WebApplication> map)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.Configuration["urls"] = "http://127.0.0.1:0";
            configure(builder);
            var app = builder.Build();
            map(app);
            await app.StartAsync();
            return new MocksHost(app);
        }

        public async Task<MockResponse> SendAsync(MockRequest request)
        {
            if (request.ClientKeys is { Length: > 1 })
            {
                return await SendRawAsync(request);
            }

            using var message = new HttpRequestMessage(new HttpMethod(request.Method), request.Path);
            if (request.Body != null)
            {
                message.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(request.Body));
                if (request.ContentType != null)
                {
                    message.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
                }
            }
            else if (request.Method == "POST")
            {
                message.Content = new ByteArrayContent([]);
            }

            if (request.ClientKeys is { Length: 1 })
            {
                message.Headers.TryAddWithoutValidation("client-key", request.ClientKeys[0]);
            }

            using var response = await client.SendAsync(message);
            var body = await response.Content.ReadAsStringAsync();
            var headers = response.Headers.Select(h => (h.Key, string.Join("|", h.Value)))
                .Concat(response.Content.Headers.Select(h => (h.Key, string.Join("|", h.Value))))
                .Where(h => h.Key is not ("Date" or "Server"))
                .OrderBy(h => h.Key, StringComparer.Ordinal)
                .Select(h => $"{h.Key}: {h.Item2}");
            var contentType = response.Content.Headers.TryGetValues("Content-Type", out var values)
                ? values.Single()
                : null;
            return Normalise(new MockResponse((int)response.StatusCode, contentType, body,
                string.Join("\n", headers)));
        }

        private async Task<MockResponse> SendRawAsync(MockRequest request)
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(baseAddress.Host, baseAddress.Port);
            var stream = tcp.GetStream();
            var body = Encoding.UTF8.GetBytes(request.Body ?? string.Empty);
            var head = new StringBuilder();
            head.Append($"{request.Method} {request.Path} HTTP/1.1\r\nHost: {baseAddress.Authority}\r\n");
            foreach (var key in request.ClientKeys.Required())
            {
                head.Append($"client-key: {key}\r\n");
            }

            if (request.ContentType != null)
            {
                head.Append($"Content-Type: {request.ContentType}\r\n");
            }

            head.Append($"Content-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(Encoding.ASCII.GetBytes(head.ToString()));
            await stream.WriteAsync(body);

            using var memory = new System.IO.MemoryStream();
            await stream.CopyToAsync(memory);
            var text = Encoding.UTF8.GetString(memory.ToArray());
            var split = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            var lines = text[..split].Split("\r\n");
            var status = int.Parse(lines[0].Split(' ')[1]);
            var headerPairs = lines.Skip(1).Select(l => l.Split(": ", 2)).ToList();
            var rest = text[(split + 4)..];
            if (headerPairs.Any(h => h[0].Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)))
            {
                var decoded = new StringBuilder();
                var position = 0;
                while (true)
                {
                    var eol = rest.IndexOf("\r\n", position, StringComparison.Ordinal);
                    var size = Convert.ToInt32(rest[position..eol], 16);
                    if (size == 0)
                    {
                        break;
                    }

                    decoded.Append(rest.AsSpan(eol + 2, size));
                    position = eol + 2 + size + 2;
                }

                rest = decoded.ToString();
            }

            var contentType = headerPairs.FirstOrDefault(h =>
                h[0].Equals("Content-Type", StringComparison.OrdinalIgnoreCase))?[1];
            var headers = headerPairs.Where(h => h[0] is not ("Date" or "Server"))
                .Select(h => $"{Canonical(h[0])}: {h[1]}").OrderBy(h => h, StringComparer.Ordinal);
            return Normalise(new MockResponse(status, contentType, rest, string.Join("\n", headers)));
        }

        private static string Canonical(string name) =>
            string.Join("-",
                name.Split('-').Select(p =>
                    p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));

        private static MockResponse Normalise(MockResponse response) =>
            response with { Body = guidPattern.Replace(response.Body, "<guid>") };
    }
}
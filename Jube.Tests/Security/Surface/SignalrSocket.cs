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
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jube.Test.Security.PenTest;

namespace Jube.Test.Security.Surface;

public sealed class SignalrSocket : IAsyncDisposable
{
    private const char Separator = '\u001e';
    private readonly ClientWebSocket socket;
    private readonly StringBuilder pending = new();

    private SignalrSocket(ClientWebSocket socket)
    {
        this.socket = socket;
    }

    public static async Task<SignalrSocket?> ConnectAsync(PenTestHost host, PenTestClient anonymous, string hub,
        string token, PenTestRun run)
    {
        var negotiate = await anonymous.WithCookie(SurfaceCookies.Header(token))
            .SendAsync("POST", hub + "/negotiate?negotiateVersion=1", []);
        if (negotiate.Status != 200)
        {
            run.Fail(negotiate, "HUB-CONNECT", "negotiate did not succeed for a legitimate user");
            return null;
        }

        using var document = JsonDocument.Parse(negotiate.Body);
        var id = document.RootElement.TryGetProperty("connectionToken", out var t)
            ? t.GetString()
            : document.RootElement.GetProperty("connectionId").GetString();
        var client = new ClientWebSocket();
        client.Options.SetRequestHeader("Cookie", SurfaceCookies.Header(token));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try
        {
            await client.ConnectAsync(new Uri($"ws://{host.BaseAddress.Authority}{hub}?id={Uri.EscapeDataString(id!)}"),
                cancellation.Token);
            var created = new SignalrSocket(client);
            await created.WriteAsync("{\"protocol\":\"json\",\"version\":1}" + Separator);
            var handshake = await created.ReadFramesAsync(TimeSpan.FromSeconds(5), 1);
            if (handshake.Count != 0 && handshake[0].StartsWith("{}", StringComparison.Ordinal))
            {
                return created;
            }

            run.Fail("HUB-HANDSHAKE", $"unexpected handshake reply: {string.Join("|", handshake)}");
            await created.DisposeAsync();
            return null;
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException)
        {
            client.Dispose();
            run.Fail("HUB-CONNECT", ex.GetType().Name + ": " + ex.Message);
            return null;
        }
    }

    public async Task<string?> InvokeAsync(string target, string argumentsJson, string invocationId)
    {
        var frame = "{\"type\":1,\"invocationId\":" + JsonSerializer.Serialize(invocationId) + ",\"target\":" +
                    JsonSerializer.Serialize(target) + ",\"arguments\":" + argumentsJson + "}" + Separator;
        if (!await TryWriteAsync(frame))
        {
            return null;
        }

        foreach (var reply in await ReadFramesAsync(TimeSpan.FromSeconds(3), 1,
                     f => f.Contains("\"type\":3", StringComparison.Ordinal)))
        {
            if (reply.Contains("\"type\":3", StringComparison.Ordinal))
            {
                return reply;
            }
        }

        return null;
    }

    public async Task<List<string>> SendRawAsync(string data, TimeSpan wait)
    {
        if (!await TryWriteAsync(data))
        {
            return [];
        }

        return await ReadFramesAsync(wait, int.MaxValue);
    }

    public Task<List<string>> ReceiveAsync(TimeSpan wait)
    {
        return ReadFramesAsync(wait, int.MaxValue);
    }

    private async Task<bool> TryWriteAsync(string data)
    {
        try
        {
            await WriteAsync(data);
            return true;
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or ObjectDisposedException
                                       or InvalidOperationException)
        {
            return false;
        }
    }

    private async Task WriteAsync(string data)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, cancellation.Token);
    }

    private async Task<List<string>> ReadFramesAsync(TimeSpan wait, int stopAfter, Func<string, bool>? stopWhen = null)
    {
        var frames = new List<string>();
        using var cancellation = new CancellationTokenSource(wait);
        var buffer = new byte[16384];
        try
        {
            while (socket.State == WebSocketState.Open && frames.Count < stopAfter ||
                   stopWhen != null && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellation.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                pending.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                var text = pending.ToString();
                int index;
                while ((index = text.IndexOf(Separator)) >= 0)
                {
                    var frame = text[..index];
                    text = text[(index + 1)..];
                    if (frame.Contains("\"type\":6", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    frames.Add(frame);
                    if (stopWhen == null || !stopWhen(frame))
                    {
                        continue;
                    }

                    pending.Clear();
                    pending.Append(text);
                    return frames;
                }

                pending.Clear();
                pending.Append(text);
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
        }

        return frames;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "done", cancellation.Token);
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
        }

        socket.Dispose();
    }
}
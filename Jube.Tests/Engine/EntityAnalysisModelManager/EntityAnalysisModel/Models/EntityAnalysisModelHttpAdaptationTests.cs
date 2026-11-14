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
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.HttpAdaptations;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Test.Infrastructure;
using Newtonsoft.Json;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EngineModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using EntityAnalysisModelHttpAdaptation =
    Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelHttpAdaptation;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    [Trait("Category", "Unit")]
    public sealed class EntityAnalysisModelHttpAdaptationTests
    {
        private static readonly JsonSerializerSettings Settings = new();

        private static async Task<(TcpListener Listener, string Url, Task<string> Request)> ServeAsync(
            Func<NetworkStream, Task> respond)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var request = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                var buffer = new byte[8192];
                var received = new StringBuilder();

                while (!received.ToString().Contains("\r\n\r\n"))
                {
                    var read = await stream.ReadAsync(buffer);

                    if (read == 0)
                    {
                        break;
                    }

                    received.Append(Encoding.ASCII.GetString(buffer, 0, read));
                }

                await respond(stream);

                return received.ToString();
            });

            await Task.CompletedTask;

            return (listener, $"http://127.0.0.1:{port}/score", request);
        }

        private static Task Write(NetworkStream stream, string status, string headers, string body)
        {
            var bytes = Encoding.UTF8.GetBytes(
                $"HTTP/1.1 {status}\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n{headers}\r\n{body}");

            return stream.WriteAsync(bytes).AsTask();
        }

        private static EntityAnalysisModelHttpAdaptation NewAdaptation(string url, int timeout = 2000)
        {
            return new EntityAnalysisModelHttpAdaptation(1, true, timeout) { Id = 1, Name = "A", HttpEndpoint = url };
        }

        private static Jube.Engine.EntityAnalysisModelInvoke.Context.Context NewContext()
        {
            return new Jube.Engine.EntityAnalysisModelInvoke.Context.Context
            {
                EntityAnalysisModel = new EngineModel(),
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload(),
                Log = TestLog.NoOp,
                Stopwatch = System.Diagnostics.Stopwatch.StartNew()
            };
        }

        [Fact]
        public async Task SuccessfulResponseBodyIsReturnedAsync()
        {
            var (listener, url, request) = await ServeAsync(s => Write(s, "200 OK", "", "0.42"));

            try
            {
                var body = await NewAdaptation(url).PostAsync(new EntityAnalysisModelInstanceEntryPayload(), Settings,
                    TestLog.NoOp);

                body.Should().Be("0.42");
                (await request).Should().StartWith("POST /score ");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task RedirectIsNotFollowedAndIsRefusedAsync()
        {
            var second = new TcpListener(IPAddress.Loopback, 0);
            second.Start();
            var secondPort = ((IPEndPoint)second.LocalEndpoint).Port;

            var (listener, url, _) = await ServeAsync(s =>
                Write(s, "302 Found", $"Location: http://127.0.0.1:{secondPort}/metadata\r\n", ""));

            try
            {
                var act = async () => await NewAdaptation(url).PostAsync(new EntityAnalysisModelInstanceEntryPayload(),
                    Settings, TestLog.NoOp);

                (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should()
                    .Be(HttpStatusCode.Found);
                second.Pending().Should().BeFalse();
            }
            finally
            {
                listener.Stop();
                second.Stop();
            }
        }

        [Fact]
        public async Task UnsuccessfulStatusThrowsInsteadOfReturningTheErrorPageAsync()
        {
            var (listener, url, _) = await ServeAsync(s => Write(s, "500 Internal Server Error", "", "<html>stack</html>"));

            try
            {
                var act = async () => await NewAdaptation(url).PostAsync(new EntityAnalysisModelInstanceEntryPayload(),
                    Settings, TestLog.NoOp);

                (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should()
                    .Be(HttpStatusCode.InternalServerError);
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task ResponseOverTheMaximumSizeIsRefusedAsync()
        {
            var big = new string('1', (int)EntityAnalysisModelHttpAdaptation.MaximumResponseBytes + 1);
            var (listener, url, _) = await ServeAsync(s => Write(s, "200 OK", "", big));

            try
            {
                var act = async () => await NewAdaptation(url).PostAsync(new EntityAnalysisModelInstanceEntryPayload(),
                    Settings, TestLog.NoOp);

                await act.Should().ThrowAsync<HttpRequestException>();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task RecallOfAnUnsuccessfulStatusIsASuppressedAdaptationNotAnExceptionAsync()
        {
            var (listener, url, _) = await ServeAsync(s => Write(s, "503 Service Unavailable", "", "down"));

            try
            {
                var adaptation = await NewContext().RecallHttpEndpointAsync(NewAdaptation(url), Settings);

                adaptation.IsSuppressed.Should().BeTrue();
                adaptation.Value.Should().BeNull();
                adaptation.Error.Should().NotBeNullOrEmpty();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task RecallOfAnUnreachableEndpointIsASuppressedAdaptationAsync()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var adaptation = await NewContext()
                .RecallHttpEndpointAsync(NewAdaptation($"http://127.0.0.1:{port}/score"), Settings);

            adaptation.IsSuppressed.Should().BeTrue();
            adaptation.Error.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task RecallOfAnEndpointThatNeverRespondsIsASuppressedAdaptationAtTheTimeoutAsync()
        {
            var (listener, url, _) = await ServeAsync(async _ => await Task.Delay(TimeSpan.FromSeconds(10)));

            try
            {
                var adaptation = await NewContext().RecallHttpEndpointAsync(NewAdaptation(url, 300), Settings);

                adaptation.IsSuppressed.Should().BeTrue();
                adaptation.Error.Should().Contain("timeout");
            }
            finally
            {
                listener.Stop();
            }
        }

        [Theory]
        [InlineData("/predict", true)]
        [InlineData("/v1/score?x=1&y=2", true)]
        [InlineData("/a/..b/c", true)]
        [InlineData("", false)]
        [InlineData("predict", false)]
        [InlineData("http://evil.example/", false)]
        [InlineData("/../etc/passwd", false)]
        [InlineData("/a/%2e%2e/b", false)]
        [InlineData("/a\\b", false)]
        [InlineData("/a b", false)]
        [InlineData("/a\r\nHost: evil", false)]
        [InlineData("/a%0d%0ab", false)]
        public void IsPlainPathAcceptsOnlyAPathBelowTheConfiguredHost(string endpoint, bool expected)
        {
            EntityAnalysisModelHttpAdaptation.IsPlainPath(endpoint).Should().Be(expected);
        }

        [Fact]
        public async Task StrictModeRefusesALoopbackTargetAtConnectTimeAsync()
        {
            var (listener, url, _) = await ServeAsync(s => Write(s, "200 OK", "", "0.42"));

            try
            {
                var adaptation = new EntityAnalysisModelHttpAdaptation(1, true, 2000, false)
                {
                    Id = 1, Name = "A", HttpEndpoint = url
                };

                var act = async () => await adaptation.PostAsync(new EntityAnalysisModelInstanceEntryPayload(),
                    Settings, TestLog.NoOp);

                (await act.Should().ThrowAsync<HttpRequestException>()).Which.Message.Should()
                    .Contain("outbound host policy");
                listener.Pending().Should().BeFalse();
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task StrictModeRefusesAHostNameThatResolvesToLoopbackAsync()
        {
            var adaptation = new EntityAnalysisModelHttpAdaptation(1, true, 2000, false)
            {
                Id = 1, Name = "A", HttpEndpoint = "http://localhost:9/score"
            };

            var act = async () => await adaptation.PostAsync(new EntityAnalysisModelInstanceEntryPayload(), Settings,
                TestLog.NoOp);

            (await act.Should().ThrowAsync<HttpRequestException>()).Which.Message.Should()
                .Contain("outbound host policy");
        }

        [Fact]
        public async Task BreakerOpensAfterRepeatedFailuresAndIsKeyedPerBreakerKeyAsync()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var resilience = new Jube.ResilientHttp.HttpResilienceOptions
            {
                Timeout = TimeSpan.FromSeconds(2),
                MinimumThroughput = 2,
                BreakDuration = TimeSpan.FromMinutes(5)
            };

            var unique = Guid.NewGuid().ToString();

            var failing = new EntityAnalysisModelHttpAdaptation(1, true, 2000)
            {
                Id = 1, Name = "A", HttpEndpoint = $"http://127.0.0.1:{port}/score",
                BreakerKey = $"{unique}-tenant-1|127.0.0.1:{port}", Resilience = resilience
            };

            for (var i = 0; i < 2; i++)
            {
                var act = async () => await failing.PostAsync(new EntityAnalysisModelInstanceEntryPayload(), Settings,
                    TestLog.NoOp);
                await act.Should().ThrowAsync<HttpRequestException>();
            }

            var open = async () => await failing.PostAsync(new EntityAnalysisModelInstanceEntryPayload(), Settings,
                TestLog.NoOp);
            await open.Should().ThrowAsync<Polly.CircuitBreaker.BrokenCircuitException>();

            var adaptation = await NewContext().RecallHttpEndpointAsync(failing, Settings);
            adaptation.Error.Should().Contain("suspended");

            var (listener, url, _) = await ServeAsync(s => Write(s, "200 OK", "", "0.42"));

            try
            {
                var other = new EntityAnalysisModelHttpAdaptation(1, true, 2000)
                {
                    Id = 2, Name = "B", HttpEndpoint = url,
                    BreakerKey = $"{unique}-tenant-2|{new Uri(url).Authority}", Resilience = resilience
                };

                (await other.PostAsync(new EntityAnalysisModelInstanceEntryPayload(), Settings, TestLog.NoOp))
                    .Should().Be("0.42");
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}

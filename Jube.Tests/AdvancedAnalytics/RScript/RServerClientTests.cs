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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.RScript;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.RScript
{
    public class RServerClientTests
    {
        private const string BaseUrl = "http://r-server.test";

        private static RServerClient NewClient(StubHttpMessageHandler handler)
        {
            return new RServerClient(new HttpClient(handler), BaseUrl);
        }

        private static HttpResponseMessage Json(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        [Fact]
        [Trait("Covers", "HealthAsync")]
        public async Task HealthAsyncCallsTheHealthEndpointAndParsesTheResponseAsync()
        {
            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {
                                                                      "status": "ok",
                                                                      "rVersion": "R version 4.4.0",
                                                                      "plumberVersion": "1.2.1",
                                                                      "upSinceUtc": "2026-01-01T00:00:00.000Z",
                                                                      "uptimeSeconds": 12.3,
                                                                      "defaultTimeoutSeconds": 30,
                                                                      "maxTimeoutSeconds": 300,
                                                                      "scriptRules": {
                                                                        "maxScriptChars": 20000,
                                                                        "maxNestingDepth": 50,
                                                                        "forbiddenFunctions": ["system", "eval"]
                                                                      }
                                                                    }
                                                                    """));

            var client = NewClient(handler);

            var result = await client.HealthAsync();

            handler.CallCount.Should().Be(1);
            handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
            handler.LastRequest.RequestUri.Should().Be(new Uri(BaseUrl + RServerClient.HealthPath));
            result.Status.Should().Be("ok");
            result.RVersion.Should().Be("R version 4.4.0");
            result.MaxTimeoutSeconds.Should().Be(300);
            result.ScriptRules!.ForbiddenFunctions.Should().Contain("eval");
        }

        [Fact]
        [Trait("Covers", "PackagesAsync")]
        public async Task PackagesAsyncCallsThePackagesEndpointAndParsesTheResponseAsync()
        {
            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {
                                                                      "count": 2,
                                                                      "packages": [{"name": "jsonlite", "version": "1.8.8"}, {"name": "dplyr", "version": "1.1.4"}],
                                                                      "note": "Every package listed here is already installed."
                                                                    }
                                                                    """));

            var client = NewClient(handler);

            var result = await client.PackagesAsync();

            handler.LastRequest!.RequestUri.Should().Be(new Uri(BaseUrl + RServerClient.PackagesPath));
            result.Count.Should().Be(2);
            result.Packages.Should().Contain(p => p.Name == "jsonlite" && p.Version == "1.8.8");
        }

        [Fact]
        [Trait("Covers", "ValidateAsync")]
        public async Task ValidateAsyncPostsTheScriptAndParsesTheResponseAsync()
        {
            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {"ok": false, "errors": ["script must be a non-empty string."], "lint": []}
                                                                    """));

            var client = NewClient(handler);

            var result = await client.ValidateAsync("bad script");

            handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
            handler.LastRequest.RequestUri.Should().Be(new Uri(BaseUrl + RServerClient.ValidatePath));
            handler.LastRequestBody.Should().Contain("\"script\":\"bad script\"");
            result.Ok.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.Should().Contain("non-empty string");
        }

        [Fact]
        [Trait("Covers", "ExecuteAsync")]
        public async Task ExecuteAsyncPostsTheScriptAndTimeoutAndParsesTheResponseAsync()
        {
            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {
                                                                      "status": "ok",
                                                                      "consoleOutput": "hello",
                                                                      "error": null,
                                                                      "warnings": ["a warning"],
                                                                      "durationMs": 42.5,
                                                                      "timedOut": false,
                                                                      "rVersion": "R version 4.4.0",
                                                                      "timestamp": "2026-01-01T00:00:00.000Z"
                                                                    }
                                                                    """));

            var client = NewClient(handler);

            var result = await client.ExecuteAsync("print('hello')", 15);

            handler.LastRequest!.RequestUri.Should().Be(new Uri(BaseUrl + RServerClient.ExecutePath));
            handler.LastRequestBody.Should().Contain("\"timeoutSeconds\":15");
            result.Status.Should().Be("ok");
            result.ConsoleOutput.Should().Be("hello");
            result.Warnings.Should().ContainSingle().Which.Should().Be("a warning");
            result.DurationMs.Should().Be(42.5);
            result.TimedOut.Should().BeFalse();
        }

        [Fact]
        [Trait("Covers", "ExecuteAsync")]
        public async Task ExecuteAsyncPassesTheCallersCancellationTokenThroughAsync()
        {
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();
            var token = cancellation.Token;

            var handler = new StubHttpMessageHandler((_, handlerToken) =>
            {
                handlerToken.ThrowIfCancellationRequested();

                return Json("should not be reached");
            });

            var client = NewClient(handler);

            var act = () => client.ExecuteAsync("1", null, token: token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
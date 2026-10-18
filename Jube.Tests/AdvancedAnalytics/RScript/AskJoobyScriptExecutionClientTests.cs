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

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.RScript;
using Jube.Service.Repository.CrossCutScriptExecution;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.RScript
{
    [Trait("Category", "AdvancedAnalytics")]
    public class AskJoobyScriptExecutionClientTests
    {
        private static HttpResponseMessage Json(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private static FakeSandboxedScriptRuns NewRuns(StubHttpMessageHandler handler)
        {
            return new FakeSandboxedScriptRuns
            {
                Server = new RServerClient(new HttpClient(handler), "http://r-server.test")
            };
        }

        [Fact]
        [Trait("Covers", "AskJoobyScriptExecutionClient.CheckHealthAsync")]
        public async Task CheckHealthMapsEveryFieldIncludingScriptRulesAsync()
        {
            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {"status":"ok","rVersion":"4.5.1","plumberVersion":"1.0",
                                                                    "upSinceUtc":"2026-01-01T00:00:00Z","uptimeSeconds":12.5,
                                                                    "defaultTimeoutSeconds":30,"maxTimeoutSeconds":300,
                                                                    "scriptRules":{"maxScriptChars":200000,"maxNestingDepth":12,
                                                                    "forbiddenFunctions":["system","shell"]}}
                                                                    """));

            var client = new AskJoobyScriptExecutionClient(NewRuns(handler));

            var result = await client.CheckHealthAsync();

            result.Status.Should().Be("ok");
            result.RVersion.Should().Be("4.5.1");
            result.UptimeSeconds.Should().Be(12.5);
            result.ScriptRules.Should().NotBeNull();
            result.ScriptRules!.MaxScriptChars.Should().Be(200000);
            result.ScriptRules.ForbiddenFunctions.Should().BeEquivalentTo("system", "shell");
        }

        [Fact]
        [Trait("Covers", "AskJoobyScriptExecutionClient.ListPackagesAsync")]
        public async Task ListPackagesMapsEveryPackageAsync()
        {
            var handler = new StubHttpMessageHandler((_, _) => Json(
                """{"count":2,"packages":[{"name":"dplyr","version":"1.1.0"},{"name":"jsonlite","version":"1.8.0"}],"note":"x"}"""));

            var client = new AskJoobyScriptExecutionClient(NewRuns(handler));

            var result = await client.ListPackagesAsync();

            result.Count.Should().Be(2);

            result.Packages.Should().SatisfyRespectively(
                first => first.Name.Should().Be("dplyr"),
                second => second.Name.Should().Be("jsonlite"));
        }

        [Fact]
        [Trait("Covers", "AskJoobyScriptExecutionClient.ValidateAsync")]
        public async Task ValidateMapsErrorsAsync()
        {
            var handler = new StubHttpMessageHandler((_, _) =>
                Json("""{"ok":false,"errors":["Forbidden function 'system' referenced at line 1."],"lint":[]}"""));

            var client = new AskJoobyScriptExecutionClient(NewRuns(handler));

            var result = await client.ValidateAsync("system('id')");

            result.Ok.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.Should().Contain("Forbidden function");
        }

        [Fact]
        [Trait("Covers", "AskJoobyScriptExecutionClient.RunScriptAsync")]
        public async Task RunScriptMapsTheFakeGatewaysResultAsync()
        {
            var runs = NewRuns(new StubHttpMessageHandler((_, _) => Json("{}")));
            var client = new AskJoobyScriptExecutionClient(runs);

            var result = await client.RunScriptAsync("print(1)", 30);

            result.Succeeded.Should().BeTrue();
            result.ConsoleOutput.Should().Be("ok");
            runs.LastRunScript.Should().Be("print(1)");
            runs.LastRunTimeoutSeconds.Should().Be(30);
        }
    }
}
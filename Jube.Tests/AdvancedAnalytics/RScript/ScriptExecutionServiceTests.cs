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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.Engine.AdvancedAnalytics.RScript;
using Jube.Service.Agent.CrossCutTools;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CrossCutScriptExecution;
using Jube.Test.AdvancedAnalytics.Service;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.RScript
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ScriptExecutionServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private static HttpResponseMessage Json(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private Task<ScriptExecutionService> ServiceAsync(string user, StubHttpMessageHandler handler,
            IToolResultCache? resultCache = null)
        {
            var rServer = new RServerClient(new HttpClient(handler), "http://r-server.test");

            return ScriptExecutionService.CreateAsync(Db, user, TestLog.NoOp, Localizers,
                new NullServiceChangeBus(), rServer, TestLog.NoOp, resultCache);
        }

        private sealed class FakeToolResultCache(IReadOnlyDictionary<string, CachedTable> tables)
            : IToolResultCache
        {
            public Task<string> StoreAsync(CachedTable table, string? label = null, TimeSpan? ttl = null,
                CancellationToken token = default)
            {
                throw new NotSupportedException("Not needed by these tests.");
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                return Task.FromResult(tables.GetValueOrDefault(pointerKey));
            }
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptReturnsTheRealConsoleOutputAndResultOnSuccessAsync()
        {
            var user = await UserWithSpecsAsync(48, 64);

            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {
                                                                      "status": "ok",
                                                                      "consoleOutput": "<<<JUBE_RESULT_START>>>{\"total\":2}<<<JUBE_RESULT_END>>>",
                                                                      "warnings": [],
                                                                      "durationMs": 12.5,
                                                                      "timedOut": false,
                                                                      "rVersion": "R version 4.5.1",
                                                                      "timestamp": "2026-01-01T00:00:00.000Z"
                                                                    }
                                                                    """));

            var service = await ServiceAsync(user, handler);

            var result = await service.RunScriptAsync("1 + 1", 5);

            result.Succeeded.Should().BeTrue();
            result.TimedOut.Should().BeFalse();
            result.Error.Should().BeNull();
            result.ResultJson.Should().Be("{\"total\":2}");
            result.DurationMs.Should().Be(12.5);
            result.RVersion.Should().Be("R version 4.5.1");
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptReportsATimeoutExactlyAsTheRServerDescribedItAsync()
        {
            var user = await UserWithSpecsAsync(48, 64);

            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {
                                                                      "status": "error",
                                                                      "consoleOutput": "",
                                                                      "error": "reached elapsed time limit",
                                                                      "warnings": [],
                                                                      "durationMs": 1000.0,
                                                                      "timedOut": true,
                                                                      "rVersion": "R version 4.5.1",
                                                                      "timestamp": "2026-01-01T00:00:00.000Z"
                                                                    }
                                                                    """));

            var service = await ServiceAsync(user, handler);

            var result = await service.RunScriptAsync("Sys.sleep(5)", 1);

            result.Succeeded.Should().BeFalse();
            result.TimedOut.Should().BeTrue();
            result.Error.Should().Be("reached elapsed time limit");
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptRefusesAnEmptyScriptWithoutCallingTheServerAsync()
        {
            var user = await UserWithSpecsAsync(48, 64);

            var handler = new StubHttpMessageHandler((_, _) =>
                throw new InvalidOperationException("The R server should never be called for an invalid request."));

            var service = await ServiceAsync(user, handler);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.RunScriptAsync(" ", 5));
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptRefusesATimeoutOutsideOneToThreeHundredSecondsAsync()
        {
            var user = await UserWithSpecsAsync(48, 64);

            var handler = new StubHttpMessageHandler((_, _) =>
                throw new InvalidOperationException("The R server should never be called for an invalid request."));

            var service = await ServiceAsync(user, handler);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.RunScriptAsync("1 + 1", 301));
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptIsForbiddenWithoutTheSandboxedScriptExecutionPermissionEvenWithAnalyticsAsync()
        {
            var handler = new StubHttpMessageHandler((_, _) =>
                throw new InvalidOperationException("The R server should never be called when forbidden."));

            var service = await ServiceAsync(Permitted, handler);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.RunScriptAsync("1 + 1", 5));
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptHonoursAnExternallyCancelledTokenRatherThanReturningAResultAsync()
        {
            var user = await UserWithSpecsAsync(48, 64);

            var handler = new StubHttpMessageHandler((_, token) =>
            {
                token.ThrowIfCancellationRequested();

                return Json("should not be reached");
            });

            var service = await ServiceAsync(user, handler);
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.RunScriptAsync("1 + 1", 5, token: cancellation.Token));
        }

        private static CachedTable OneRowTable(string columnName, object? value)
        {
            return new CachedTable(
                [new CachedColumn(columnName, value?.GetType().Name ?? "unknown")],
                [new Dictionary<string, object?> { [columnName] = value }]);
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptResolvesAPointerBelongingToTheCallersOwnTenantAndForwardsItToRAsync()
        {
            var user = await UserWithSpecsAsync(48, 64);

            var cache = new FakeToolResultCache(new Dictionary<string, CachedTable>
            {
                ["tbl_mine"] = OneRowTable("RuleName", "velocity-check")
            });

            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {
                                                                      "status": "ok",
                                                                      "consoleOutput": "ok",
                                                                      "warnings": [],
                                                                      "durationMs": 1.0,
                                                                      "timedOut": false,
                                                                      "rVersion": "R version 4.5.1",
                                                                      "timestamp": "2026-01-01T00:00:00.000Z"
                                                                    }
                                                                    """));

            var service = await ServiceAsync(user, handler, cache);

            var result = await service.RunScriptAsync("1 + 1", 5,
                [new RScriptDataframeRequest("rules", "tbl_mine")]);

            result.Succeeded.Should().BeTrue();
            handler.CallCount.Should().Be(1);
            handler.LastRequestBody.Should().Contain("\"name\":\"rules\"");
            handler.LastRequestBody.Should().Contain("RuleName");
            handler.LastRequestBody.Should().Contain("velocity-check");
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptPutsTheCallersUserNameOnTheOutboundRequestAsync()
        {
            var user = await UserWithSpecsAsync(48, 64);

            var handler = new StubHttpMessageHandler((_, _) => Json("""
                                                                    {
                                                                      "status": "ok",
                                                                      "consoleOutput": "ok",
                                                                      "warnings": [],
                                                                      "durationMs": 1.0,
                                                                      "timedOut": false,
                                                                      "rVersion": "R version 4.5.1",
                                                                      "timestamp": "2026-01-01T00:00:00.000Z"
                                                                    }
                                                                    """));

            var service = await ServiceAsync(user, handler);

            await service.RunScriptAsync("1 + 1", 5);

            handler.LastRequestBody.Should().Contain($"\"userName\":\"{user}\"");
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptFailsWithAGenericErrorForAnUnresolvablePointerWithoutEverCallingRAsync()
        {
            var user = await UserWithSpecsAsync(48, 64);
            var cache = new FakeToolResultCache(new Dictionary<string, CachedTable>());

            var handler = new StubHttpMessageHandler((_, _) =>
                throw new InvalidOperationException(
                    "The R server should never be called once a dataframe pointer fails to resolve."));

            var service = await ServiceAsync(user, handler, cache);

            var act = () => service.RunScriptAsync("1 + 1", 5,
                [new RScriptDataframeRequest("rules", "tbl_wrong_tenant_or_missing")]);

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        [Trait("Covers", "ScriptExecutionService.RunScriptAsync")]
        public async Task RunScriptFailsAllDataframesWhenOnlyOneOfSeveralCannotBeResolvedAsync()
        {
            var user = await UserWithSpecsAsync(48, 64);

            var cache = new FakeToolResultCache(new Dictionary<string, CachedTable>
            {
                ["tbl_good"] = OneRowTable("X", 1.0)
            });

            var handler = new StubHttpMessageHandler((_, _) =>
                throw new InvalidOperationException(
                    "The R server should never be called when any one dataframe pointer fails to resolve."));

            var service = await ServiceAsync(user, handler, cache);

            var act = () => service.RunScriptAsync("1 + 1", 5,
            [
                new RScriptDataframeRequest("good", "tbl_good"),
                new RScriptDataframeRequest("bad", "tbl_does_not_exist")
            ]);

            await act.Should().ThrowAsync<DtoValidationException>();
        }
    }
}
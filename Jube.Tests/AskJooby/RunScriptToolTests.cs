/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Tools.ScriptExecution.Interfaces;
using Jube.AskJooby.ScriptExecution.Models;
using Jube.AskJooby.Tools.ScriptExecution;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class RunScriptToolTests
    {
        private sealed class FakeScriptExecutionClient : IScriptExecutionClient
        {
            public RServerHealthResult? Health { get; set; }
            public RServerPackagesResult? Packages { get; set; }
            public RServerValidateResult? Validation { get; set; }
            public RunScriptResult? RunResult { get; set; }
            public InvalidOperationException? Throws { get; set; }

            public Task<RServerHealthResult> CheckHealthAsync(CancellationToken token = default)
            {
                return Throws is null ? Task.FromResult(Health!) : throw Throws;
            }

            public Task<RServerPackagesResult> ListPackagesAsync(CancellationToken token = default)
            {
                return Throws is null ? Task.FromResult(Packages!) : throw Throws;
            }

            public Task<RServerValidateResult> ValidateAsync(string script, CancellationToken token = default)
            {
                return Throws is null ? Task.FromResult(Validation!) : throw Throws;
            }

            public Task<RunScriptResult> RunScriptAsync(string script, int? timeoutSeconds,
                IReadOnlyList<RScriptDataframeRequest>? dataframes = null,
                IReadOnlyList<RScriptVariableRequest>? variables = null, CancellationToken token = default)
            {
                return Throws is null ? Task.FromResult(RunResult!) : throw Throws;
            }
        }

        [Fact]
        public async Task CheckRServerHealthReportsStatusAndScriptRulesAsync()
        {
            var client = new FakeScriptExecutionClient
            {
                Health = new RServerHealthResult
                {
                    Status = "ok",
                    RVersion = "4.5.1",
                    PlumberVersion = "1.0",
                    UpSinceUtc = "2026-01-01T00:00:00Z",
                    UptimeSeconds = 12,
                    DefaultTimeoutSeconds = 30,
                    MaxTimeoutSeconds = 300,
                    ScriptRules = new RServerScriptRules
                    {
                        MaxScriptChars = 200000, MaxNestingDepth = 12, ForbiddenFunctions = ["system"]
                    }
                }
            };

            var tool = new RunScriptTool(client);

            var result = await tool.CheckRServerHealth();

            result.Should().Contain("Status: ok");
            result.Should().Contain("system");
        }

        [Fact]
        public async Task ListRPackagesReportsEachPackageAsync()
        {
            var client = new FakeScriptExecutionClient
            {
                Packages = new RServerPackagesResult
                {
                    Count = 1,
                    Packages = [new RServerPackageInfo { Name = "dplyr", Version = "1.1.0" }],
                    Note = string.Empty
                }
            };

            var tool = new RunScriptTool(client);

            var result = await tool.ListRPackages();

            result.Should().Contain("dplyr 1.1.0");
        }

        [Fact]
        public async Task ValidateRScriptReportsErrorsWhenNotOkAsync()
        {
            var client = new FakeScriptExecutionClient
            {
                Validation = new RServerValidateResult
                {
                    Ok = false, Errors = ["Forbidden function 'system' referenced at line 1."]
                }
            };

            var tool = new RunScriptTool(client);

            var result = await tool.ValidateRScript("system('id')");

            result.Should().Contain("Not valid");
            result.Should().Contain("Forbidden function");
        }

        [Fact]
        public async Task RunScriptReportsConsoleOutputOnSuccessAsync()
        {
            var client = new FakeScriptExecutionClient
            {
                RunResult = new RunScriptResult
                {
                    Succeeded = true, ConsoleOutput = "[1] 2", DurationMs = 12.3, RVersion = "4.5.1"
                }
            };

            var tool = new RunScriptTool(client);

            var result = await tool.RunScript("print(1+1)");

            result.Should().Contain("Succeeded");
            result.Should().Contain("[1] 2");
        }

        [Fact]
        public async Task RunScriptReportsTheErrorOnFailureAsync()
        {
            var client = new FakeScriptExecutionClient
            {
                RunResult = new RunScriptResult { Succeeded = false, Error = "The script exceeded its timeout." }
            };

            var tool = new RunScriptTool(client);

            var result = await tool.RunScript("while(TRUE){}");

            result.Should().Contain("Failed");
            result.Should().Contain("exceeded its timeout");
        }

        [Fact]
        public async Task RunScriptReturnsAFriendlyRejectionInsteadOfThrowingAsync()
        {
            var client = new FakeScriptExecutionClient
            {
                Throws = new InvalidOperationException("could not resolve dataframe pointer: transactions")
            };

            var tool = new RunScriptTool(client);

            var result = await tool.RunScript("print(nrow(transactions))");

            result.Should().Contain("could not resolve dataframe pointer");
        }
    }
}
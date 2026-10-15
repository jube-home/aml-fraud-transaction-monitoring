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
using System.Threading;
using System.Threading.Tasks;
using Jube.Engine.AdvancedAnalytics.RScript;
using Jube.Service.Agent.CrossCutTools;
using Jube.Service.Repository.CrossCutScriptExecution.Interfaces;
using Jube.Engine.AdvancedAnalytics.RScript.Models;

namespace Jube.Test.AdvancedAnalytics.RScript
{
    public sealed class FakeSandboxedScriptRuns : ISandboxedScriptRuns
    {
        public RServerClient? Server { get; set; }

        public Task<RServerHealthResult> CheckHealthAsync(CancellationToken token = default)
        {
            return ServerOrThrow().HealthAsync(token: token);
        }

        public Task<RServerPackagesResult> ListPackagesAsync(CancellationToken token = default)
        {
            return ServerOrThrow().PackagesAsync(token: token);
        }

        public Task<RServerValidateResult> ValidateAsync(string script, CancellationToken token = default)
        {
            return ServerOrThrow().ValidateAsync(script, token: token);
        }

        public string? LastRunScript { get; private set; }
        public int? LastRunTimeoutSeconds { get; private set; }
        public IReadOnlyList<RScriptDataframeRequest>? LastRunDataframes { get; private set; }
        public IReadOnlyList<RScriptVariableRequest>? LastRunVariables { get; private set; }

        public Task<RunScriptResult> RunScriptAsync(string script, int? timeoutSeconds,
            IReadOnlyList<RScriptDataframeRequest>? dataframes = null,
            IReadOnlyList<RScriptVariableRequest>? variables = null, CancellationToken token = default)
        {
            LastRunScript = script;
            LastRunTimeoutSeconds = timeoutSeconds;
            LastRunDataframes = dataframes;
            LastRunVariables = variables;

            return Task.FromResult(new RunScriptResult
            {
                Succeeded = true,
                ConsoleOutput = "ok"
            });
        }

        private RServerClient ServerOrThrow()
        {
            return Server ?? throw new InvalidOperationException("The fake has no R server client to forward to.");
        }
    }
}
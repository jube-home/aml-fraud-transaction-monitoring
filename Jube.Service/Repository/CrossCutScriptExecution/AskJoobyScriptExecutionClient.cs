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

using Jube.AskJooby.ScriptExecution.Models;
using Jube.AskJooby.Tools.ScriptExecution.Interfaces;
using Jube.Service.Exceptions;
using Jube.Service.Repository.CrossCutScriptExecution.Interfaces;
using CrossCutRun = Jube.Service.Agent.CrossCutTools;

namespace Jube.Service.Repository.CrossCutScriptExecution
{
    public sealed class AskJoobyScriptExecutionClient(ISandboxedScriptRuns runs) : IScriptExecutionClient
    {
        public async Task<RServerHealthResult> CheckHealthAsync(CancellationToken token = default)
        {
            var result = await RunAsync(() => runs.CheckHealthAsync(token)).ConfigureAwait(false);

            return new RServerHealthResult
            {
                Status = result.Status,
                RVersion = result.RVersion,
                PlumberVersion = result.PlumberVersion,
                UpSinceUtc = result.UpSinceUtc,
                UptimeSeconds = result.UptimeSeconds,
                DefaultTimeoutSeconds = result.DefaultTimeoutSeconds,
                MaxTimeoutSeconds = result.MaxTimeoutSeconds,
                ScriptRules = result.ScriptRules is { } rules
                    ? new RServerScriptRules
                    {
                        MaxScriptChars = rules.MaxScriptChars,
                        MaxNestingDepth = rules.MaxNestingDepth,
                        ForbiddenFunctions = rules.ForbiddenFunctions
                    }
                    : null
            };
        }

        public async Task<RServerPackagesResult> ListPackagesAsync(CancellationToken token = default)
        {
            var result = await RunAsync(() => runs.ListPackagesAsync(token)).ConfigureAwait(false);

            return new RServerPackagesResult
            {
                Count = result.Count,
                Packages = result.Packages
                    .Select(package => new RServerPackageInfo { Name = package.Name, Version = package.Version })
                    .ToList(),
                Note = result.Note
            };
        }

        public async Task<RServerValidateResult> ValidateAsync(string script, CancellationToken token = default)
        {
            var result = await RunAsync(() => runs.ValidateAsync(script, token)).ConfigureAwait(false);

            return new RServerValidateResult
            {
                Ok = result.Ok,
                Errors = result.Errors,
                Lint = result.Lint
            };
        }

        public async Task<RunScriptResult> RunScriptAsync(string script, int? timeoutSeconds,
            IReadOnlyList<RScriptDataframeRequest>? dataframes = null,
            IReadOnlyList<RScriptVariableRequest>? variables = null, CancellationToken token = default)
        {
            var result = await RunAsync(() => runs.RunScriptAsync(script, timeoutSeconds,
                dataframes?.Select(dataframe =>
                    new CrossCutRun.RScriptDataframeRequest(dataframe.Name, dataframe.Pointer)).ToList(),
                variables?.Select(variable =>
                    new CrossCutRun.RScriptVariableRequest(variable.Name, variable.Value)).ToList(),
                token)).ConfigureAwait(false);

            return new RunScriptResult
            {
                Succeeded = result.Succeeded,
                TimedOut = result.TimedOut,
                ConsoleOutput = result.ConsoleOutput,
                ResultJson = result.ResultJson,
                Warnings = result.Warnings,
                DurationMs = result.DurationMs,
                RVersion = result.RVersion,
                Error = result.Error
            };
        }

        private static async Task<T> RunAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (ServiceException ex)
            {
                throw new InvalidOperationException(ex.Message, ex);
            }
        }
    }
}
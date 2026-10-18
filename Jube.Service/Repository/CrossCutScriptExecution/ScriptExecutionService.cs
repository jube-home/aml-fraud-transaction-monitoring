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

using System.Text.Json;
using FluentValidation.Results;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.Data.Context;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.RScript;
using Jube.Engine.AdvancedAnalytics.RScript.Models;
using Jube.Resources;
using Jube.Service.Agent.CrossCutTools;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Repository.CrossCutScriptExecution
{
    public sealed class ScriptExecutionService
    {
        private const int MaxScriptChars = 200000;
        private const int MinTimeoutSeconds = 1;
        private const int MaxTimeoutSeconds = 300;
        private const int TimeoutGraceSeconds = 10;
        private const int MaxErrorLength = 2000;
        private const int MaxDataframes = 10;
        private const int MaxVariables = 50;
        private const int MaxVariableStringChars = 200000;
        private const int MaxVariableVectorElements = 100000;
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly RServerClient rServer;
        private readonly IToolResultCache? resultCache;

        private ScriptExecutionService(AdvancedAnalyticsOperationRunner runner, RServerClient rServer,
            IToolResultCache? resultCache)
        {
            this.runner = runner;
            this.rServer = rServer;
            this.resultCache = resultCache;
        }

        public static Task<ScriptExecutionService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            RServerClient rServer, IToolResultCache? resultCache = null, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, rServer,
                LogManager.GetLogger("Jube.Audit"), resultCache, token);
        }

        internal static async Task<ScriptExecutionService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, RServerClient rServer, ILog auditLog,
            IToolResultCache? resultCache = null, CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                    CrossCutChangeAreas.ScriptExecution,
                    typeof(AdvancedAnalyticsTrainingResources), AdvancedAnalyticsTrainingResources.NotAuthenticated,
                    dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token)
                .ConfigureAwait(false);

            return new ScriptExecutionService(runner, rServer, resultCache);
        }

        public Task<RServerHealthResult> CheckHealthAsync(CancellationToken token = default)
        {
            return runner.RunAsync("CheckRServerHealth", _ =>
            {
                runner.EnsurePermitted(
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "CheckRServerHealth",
                    AdvancedAnalyticsTrainingResources.PermissionDenied);

                return rServer.HealthAsync(token: token);
            });
        }

        public Task<RServerPackagesResult> ListPackagesAsync(CancellationToken token = default)
        {
            return runner.RunAsync("ListRPackages", _ =>
            {
                runner.EnsurePermitted(
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "ListRPackages",
                    AdvancedAnalyticsTrainingResources.PermissionDenied);

                return rServer.PackagesAsync(token: token);
            });
        }

        public Task<RServerValidateResult> ValidateAsync(string script, CancellationToken token = default)
        {
            return runner.RunAsync("ValidateRScript", _ =>
            {
                runner.EnsurePermitted(
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "ValidateRScript",
                    AdvancedAnalyticsTrainingResources.PermissionDenied);

                return rServer.ValidateAsync(script, token: token);
            });
        }

        public Task<RunScriptResult> RunScriptAsync(string script, int? timeoutSeconds,
            IReadOnlyList<RScriptDataframeRequest>? dataframes = null,
            IReadOnlyList<RScriptVariableRequest>? variables = null,
            CancellationToken token = default)
        {
            return runner.RunAsync("RunScript", async _ =>
            {
                runner.EnsurePermitted(
                    [CrossCutPermissions.SandboxedScriptExecutionId], "RunScript",
                    AdvancedAnalyticsTrainingResources.PermissionDenied);

                ValidateRunRequest(script, timeoutSeconds, dataframes, variables);

                var resolvedDataframes = await ResolveDataframesAsync(dataframes, token).ConfigureAwait(false);

                using var guard =
                    new CancellationTokenSource(TimeSpan.FromSeconds((timeoutSeconds ?? MaxTimeoutSeconds) +
                                                                     TimeoutGraceSeconds));

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, guard.Token);

                var result = await rServer.ExecuteAsync(script, timeoutSeconds, runner.UserName,
                        resolvedDataframes,
                        variables?.Select(v => new RServerVariableRequest(v.Name, NormalizeVariableValue(v.Value)))
                            .ToList(),
                        token: linked.Token)
                    .ConfigureAwait(false);

                var parsed = RScriptResultParser.Parse(result.ConsoleOutput);
                var warnings = new List<string>(result.Warnings);

                if (parsed.Warning != null)
                {
                    warnings.Add(parsed.Warning);
                }

                var warningsJson = warnings.Count == 0 ? null : AdvancedAnalyticsEvidenceJson.Serialise(warnings);
                var succeeded = string.Equals(result.Status, "ok", StringComparison.OrdinalIgnoreCase);

                var error = succeeded
                    ? null
                    : Truncate(result.TimedOut
                        ? result.Error ?? "The script exceeded its timeout and was stopped."
                        : result.Error ?? $"The script finished with status '{result.Status}'.");

                return new RunScriptResult
                {
                    Succeeded = succeeded,
                    TimedOut = result.TimedOut,
                    ConsoleOutput = result.ConsoleOutput,
                    ResultJson = parsed.ResultJson,
                    Warnings = warningsJson,
                    DurationMs = result.DurationMs,
                    RVersion = result.RVersion,
                    Error = error
                };
            });
        }

        private async Task<IReadOnlyList<RServerDataframeRequest>?> ResolveDataframesAsync(
            IReadOnlyList<RScriptDataframeRequest>? dataframes, CancellationToken token)
        {
            if (dataframes is null || dataframes.Count == 0)
            {
                return null;
            }

            var resolved = new List<RServerDataframeRequest>(dataframes.Count);

            foreach (var dataframe in dataframes)
            {
                var table = resultCache is null
                    ? null
                    : await resultCache.GetAsync(dataframe.Pointer, token).ConfigureAwait(false);

                if (table is null)
                {
                    throw new DtoValidationException(new ValidationResult([
                        new ValidationFailure(nameof(dataframes),
                            $"could not resolve dataframe pointer: {dataframe.Name}")
                        {
                            ErrorCode = "DataframePointerNotFound"
                        }
                    ]));
                }

                resolved.Add(new RServerDataframeRequest(dataframe.Name,
                    new JRaw(JsonSerializer.Serialize(table.Columns)),
                    new JRaw(JsonSerializer.Serialize(table.Rows))));
            }

            return resolved;
        }

        private static readonly System.Text.RegularExpressions.Regex rIdentifier =
            new(@"^(?:[a-zA-Z]|\.(?![0-9]))[a-zA-Z0-9._]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static void ValidateRunRequest(string script, int? timeoutSeconds,
            IReadOnlyList<RScriptDataframeRequest>? dataframes, IReadOnlyList<RScriptVariableRequest>? variables)
        {
            var failures = new List<ValidationFailure>();

            if (string.IsNullOrWhiteSpace(script))
            {
                failures.Add(new ValidationFailure(nameof(script),
                    "A script is required.") { ErrorCode = "ScriptRequired" });
            }
            else if (script.Length > MaxScriptChars)
            {
                failures.Add(new ValidationFailure(nameof(script),
                    $"The script must be at most {MaxScriptChars} characters.")
                {
                    ErrorCode = "ScriptTooLarge"
                });
            }

            if (timeoutSeconds is < MinTimeoutSeconds or > MaxTimeoutSeconds)
            {
                failures.Add(new ValidationFailure(nameof(timeoutSeconds),
                    $"timeoutSeconds must be between {MinTimeoutSeconds} and {MaxTimeoutSeconds} when given.")
                {
                    ErrorCode = "TimeoutSecondsInvalid"
                });
            }

            if (dataframes is { Count: > MaxDataframes })
            {
                failures.Add(new ValidationFailure(nameof(dataframes),
                    $"At most {MaxDataframes} dataframes may be hydrated in one call.")
                {
                    ErrorCode = "TooManyDataframes"
                });
            }

            foreach (var dataframe in dataframes ?? [])
            {
                if (string.IsNullOrWhiteSpace(dataframe.Name) || !rIdentifier.IsMatch(dataframe.Name))
                {
                    failures.Add(new ValidationFailure(nameof(dataframes),
                        $"'{dataframe.Name}' is not a valid R identifier for a dataframe name.")
                    {
                        ErrorCode = "DataframeNameInvalid"
                    });
                }

                if (string.IsNullOrWhiteSpace(dataframe.Pointer))
                {
                    failures.Add(new ValidationFailure(nameof(dataframes),
                            $"The dataframe '{dataframe.Name}' needs a pointer.")
                        { ErrorCode = "DataframePointerRequired" });
                }
            }

            if (variables is { Count: > MaxVariables })
            {
                failures.Add(new ValidationFailure(nameof(variables),
                    $"At most {MaxVariables} variables may be bound in one call.")
                {
                    ErrorCode = "TooManyVariables"
                });
            }

            foreach (var variable in variables ?? [])
            {
                if (string.IsNullOrWhiteSpace(variable.Name) || !rIdentifier.IsMatch(variable.Name))
                {
                    failures.Add(new ValidationFailure(nameof(variables),
                        $"'{variable.Name}' is not a valid R identifier for a variable name.")
                    {
                        ErrorCode = "VariableNameInvalid"
                    });
                }

                if (variable.Value is null)
                {
                    failures.Add(new ValidationFailure(nameof(variables),
                        $"The variable '{variable.Name}' has no value -- there is no single R type a JSON " +
                        "null could become, so send a real value or omit the variable.")
                    {
                        ErrorCode = "VariableValueRequired"
                    });

                    continue;
                }

                if (!VariableValueWithinLimits(variable.Value, out var reason))
                {
                    failures.Add(new ValidationFailure(nameof(variables),
                        $"The variable '{variable.Name}' is too large: {reason}") { ErrorCode = "VariableTooLarge" });
                }
            }

            if (failures.Count > 0)
            {
                throw new DtoValidationException(new ValidationResult(failures));
            }
        }

        private static bool VariableValueWithinLimits(object value, out string reason)
        {
            if (value is JsonElement { ValueKind: JsonValueKind.Array } array)
            {
                if (array.GetArrayLength() > MaxVariableVectorElements)
                {
                    reason = $"at most {MaxVariableVectorElements} elements are allowed in one variable's array.";

                    return false;
                }

                foreach (var element in array.EnumerateArray())
                {
                    if (!VariableValueWithinLimits(element, out reason))
                    {
                        return false;
                    }
                }

                reason = "";

                return true;
            }

            if (value is JsonElement { ValueKind: JsonValueKind.String } text &&
                (text.GetString()?.Length ?? 0) > MaxVariableStringChars)
            {
                reason = $"at most {MaxVariableStringChars} characters are allowed in one variable's string.";

                return false;
            }

            reason = "";

            return true;
        }

        private static object? NormalizeVariableValue(object? value)
        {
            if (value is not JsonElement element)
            {
                return value;
            }

            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray().Select(e => NormalizeVariableValue(e)).ToList(),
                _ => null
            };
        }

        private static string Truncate(string message)
        {
            return message.Length <= MaxErrorLength ? message : message[..MaxErrorLength];
        }
    }
}
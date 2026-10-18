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

using System.ComponentModel;
using System.Text;
using Jube.AskJooby.Tools.ScriptExecution.Interfaces;
using Jube.AskJooby.ScriptExecution.Models;

namespace Jube.AskJooby.Tools.ScriptExecution
{
    public sealed class RunScriptTool(IScriptExecutionClient client)
    {
        [Description(
            "Reads the liveness, version and current script-safety rules (size limit, nesting depth limit, forbidden functions) of the sandboxed R execution server. " +
            "Use it to confirm the server is reachable and to learn its real, current limits before writing a script against it.")]
        public async Task<string> CheckRServerHealth(CancellationToken cancellationToken = default)
        {
            RServerHealthResult health;

            try
            {
                health = await client.CheckHealthAsync(cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return $"Cannot reach the R server: {ex.Message}";
            }

            var builder = new StringBuilder();
            builder.Append($"Status: {health.Status}. R {health.RVersion}, Plumber {health.PlumberVersion}, ");
            builder.Append($"up since {health.UpSinceUtc} ({health.UptimeSeconds:0.#}s). ");

            builder.Append(
                $"Timeout default {health.DefaultTimeoutSeconds:0.#}s, max {health.MaxTimeoutSeconds:0.#}s.");

            if (health.ScriptRules is { } rules)
            {
                builder.Append($" Script rules: max {rules.MaxScriptChars:N0} characters, max nesting depth ");
                builder.Append($"{rules.MaxNestingDepth}, forbidden functions: ");

                builder.Append(rules.ForbiddenFunctions.Count == 0
                    ? "none."
                    : string.Join(", ", rules.ForbiddenFunctions) + ".");
            }

            return builder.ToString();
        }

        [Description(
            "Lists every package already installed on the sandboxed R execution server, so a script can be written around what is actually available with library(<name>) rather than discovering a missing package only when it is run.")]
        public async Task<string> ListRPackages(CancellationToken cancellationToken = default)
        {
            RServerPackagesResult packages;

            try
            {
                packages = await client.ListPackagesAsync(cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return $"Cannot reach the R server: {ex.Message}";
            }

            if (packages.Count == 0)
            {
                return $"No packages reported. {packages.Note}".TrimEnd();
            }

            var list = string.Join(", ", packages.Packages.Select(package => $"{package.Name} {package.Version}"));

            return $"{packages.Count} package(s) installed: {list}. {packages.Note}".TrimEnd();
        }

        [Description(
            "Checks an R script's shape, syntax and use of disallowed functions without running it, plus non-blocking style feedback. " +
            "This is cheap and meant to be called repeatedly in a validate-then-fix loop, the same way a compiler error would be read and corrected, before ever running the script.")]
        public async Task<string> ValidateRScript(
            [Description("The R source to check.")]
            string script,
            CancellationToken cancellationToken = default)
        {
            RServerValidateResult validation;

            try
            {
                validation = await client.ValidateAsync(script, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return $"Not valid: {ex.Message}";
            }

            if (validation.Ok)
            {
                return validation.Lint.Count == 0
                    ? "Valid. No style feedback."
                    : $"Valid, with {validation.Lint.Count} style note(s): "
                      + string.Join("; ", validation.Lint.Select(entry => string.Join(", ",
                          entry.Select(pair => $"{pair.Key}={pair.Value}"))));
            }

            return $"Not valid: {string.Join("; ", validation.Errors)}";
        }

        [Description(
            "Runs an R script in the sandbox and blocks until the real result is back: nothing is queued and nothing is taken as done until this call returns. " +
            "Never use this to train, fit or search for a classifier, regressor, clusterer or anomaly model -- that is what Advanced Analytics and Exhaustive are for, and fitting one here instead skips the instance, the promotion evidence and the Exhaustive comparison those platforms give you for free. This tool is for exploring, shaping and summarising data the platform does not already have a tool for -- a dirty, quick, one-off or exploratory look, or a small cross-cutting program no purpose-built tool covers -- not for building a model that has to end up in production. " +
            "Confirm with the caller before running, unless they have already asked for exactly this kind of ad-hoc or exploratory work, or have already said as much as a standing preference. It is valuable precisely because it is unrestricted, so never reach for it as a quiet workaround -- not because a model-risk or model-admin specialist declined a production request, and not to skip the extra steps Advanced Analytics or Exhaustive would otherwise ask for. " +
            "Validate the script first with ValidateRScript, because a script that fails validation is never worth running. " +
            "To work on real data, pass pointers (never raw rows) in dataframes -- each one is hydrated into a same-named data.frame in the script's own environment before it runs, scoped to your own tenant automatically; you cannot choose whose data it reads. " +
            "Plain values that are not worth caching a pointer for (a threshold, a label, a short list) can be passed in variables instead, bound as ordinary R variables. " +
            "If any dataframe pointer cannot be resolved, nothing runs and you get a single generic error -- it does not say whether the pointer was wrong, expired, or never yours. " +
            "The sandbox has no graphics device of any kind: a script that calls a plotting function, or opens a png/pdf/svg device itself, fails on that call rather than on the fit that came before it. Never write a script that plots -- print a tree, a table or a summary as text instead, because no image this produces can ever reach the caller. " +
            "The result carries whether it succeeded, the complete console output, any warnings, how long it took, the R version it ran on, the structured result extracted from between its result markers when the script printed one, and, on failure, the error and whether it was a timeout.")]
        public async Task<string> RunScript(
            [Description("The R source to run.")] string script,
            [Description(
                "Wall-clock and CPU limit for the run in seconds, from 1 to 300, enforced by the R server itself. Leave empty to use the R server's own default.")]
            int? timeoutSeconds = null,
            [Description(
                "Pointers to hydrate into same-named data.frames before the script runs. Up to 10 per call. " +
                "Use pointers returned by a caching tool such as CacheAssembledTable or a SQL query tool.")]
            IReadOnlyList<RScriptDataframeRequest>? dataframes = null,
            [Description(
                "Plain base-type values to bind as ordinary R variables before the script runs. Up to 50 per call. " +
                "Do not send a null value -- omit the variable instead.")]
            IReadOnlyList<RScriptVariableRequest>? variables = null,
            CancellationToken cancellationToken = default)
        {
            RunScriptResult result;

            try
            {
                result = await client.RunScriptAsync(script, timeoutSeconds, dataframes, variables,
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return $"Could not run the script: {ex.Message}";
            }

            var builder = new StringBuilder();

            if (result.Succeeded)
            {
                builder.Append("Succeeded");
                builder.Append(result.DurationMs is { } durationMs ? $" in {durationMs:0}ms" : string.Empty);
                builder.Append(result.RVersion is { } version ? $" on R {version}." : ".");
            }
            else
            {
                builder.Append(result.TimedOut ? "Timed out: " : "Failed: ");
                builder.Append(result.Error ?? "no error message was returned.");
            }

            if (!string.IsNullOrEmpty(result.ConsoleOutput))
            {
                builder.Append("\nConsole output:\n").Append(result.ConsoleOutput);
            }

            if (!string.IsNullOrEmpty(result.ResultJson))
            {
                builder.Append("\nStructured result:\n").Append(result.ResultJson);
            }

            if (!string.IsNullOrEmpty(result.Warnings))
            {
                builder.Append("\nWarnings:\n").Append(result.Warnings);
            }

            return builder.ToString();
        }
    }
}
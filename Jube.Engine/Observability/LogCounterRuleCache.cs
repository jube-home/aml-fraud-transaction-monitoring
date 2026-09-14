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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository;
using log4net;

namespace Jube.Engine.Observability
{
    public sealed class LogCounterRuleCache(string connectionString, ILog log)
    {
        private const int PollIntervalMilliseconds = 60000;
        private const int RetryDelayMilliseconds = 5000;
        private static readonly TimeSpan regexMatchTimeout = TimeSpan.FromSeconds(1);
        private readonly ConcurrentDictionary<string, Counter<long>> counters = new(StringComparer.Ordinal);
        private volatile (string Name, Regex Regex)[] rules = [];

        public OpenTelemetryExcludeCache ExcludeCache { private get; set; }

        public void CheckAndIncrement(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var snapshot = rules;
            if (snapshot.Length == 0)
            {
                return;
            }

            foreach (var rule in snapshot)
            {
                bool isMatch;
                try
                {
                    isMatch = rule.Regex.IsMatch(text);
                }
                catch (RegexMatchTimeoutException)
                {
                    log.Warn($"LogCounterRuleCache: rule '{rule.Name}' timed out matching a log line and was skipped.");
                    continue;
                }

                if (!isMatch)
                {
                    continue;
                }

                if (ExcludeCache?.IsExcluded(rule.Name) == true)
                {
                    continue;
                }

                var counter = counters.GetOrAdd(rule.Name, name => EngineDiagnostics.Meter.CreateCounter<long>(name));
                counter.Add(1);
            }
        }

        public async Task StartAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RefreshAsync(token).ConfigureAwait(false);
                    await Task.Delay(PollIntervalMilliseconds, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    log.Error($"LogCounterRuleCache: Could not refresh rules: {ex}. Waiting for retry.");

                    try
                    {
                        await Task.Delay(RetryDelayMilliseconds, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task RefreshAsync(CancellationToken token)
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString, log);
            var repository = new OpenTelemetryLogCounterRepository(dbContext);

            var rows = await repository.GetAllActiveAsync(token).ConfigureAwait(false);

            var refreshed = new List<(string Name, Regex Regex)>(rows.Count);
            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.Name) || string.IsNullOrEmpty(row.Regex))
                {
                    continue;
                }

                try
                {
                    refreshed.Add((row.Name, new Regex(row.Regex, RegexOptions.Compiled, regexMatchTimeout)));
                }
                catch (ArgumentException ex)
                {
                    log.Warn(
                        $"LogCounterRuleCache: rule '{row.Name}' has an invalid Regex and was skipped: {ex.Message}");
                }
            }

            rules = refreshed.ToArray();
        }
    }
}
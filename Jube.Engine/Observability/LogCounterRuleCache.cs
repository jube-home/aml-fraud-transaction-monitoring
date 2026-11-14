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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Helpers;
using Jube.Data.Repository;
using log4net;

namespace Jube.Engine.Observability
{
    public sealed class LogCounterRuleCache(string connectionString, ILog log)
    {
        public OpenTelemetryExcludeCache ExcludeCache { private get; set; }

        public void CheckAndIncrement(string text)
        {
            ruleSet.Match(text, IsExcluded, Increment, Warn);
        }

        public void ApplyRules(IEnumerable<(string Name, string Pattern)> definitions)
        {
            ruleSet.Apply(definitions, Warn);
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

        private const int PollIntervalMilliseconds = 60000;
        private const int RetryDelayMilliseconds = 5000;
        private readonly ConcurrentDictionary<string, Counter<long>> counters = new(StringComparer.Ordinal);
        private readonly LogCounterRuleSet ruleSet = new();

        private bool IsExcluded(string name)
        {
            return ExcludeCache?.IsExcluded(name) == true;
        }

        private void Increment(string name)
        {
            counters.GetOrAdd(name, key => EngineDiagnostics.Meter.CreateCounter<long>(key)).Add(1);
        }

        private void Warn(string message)
        {
            log.Warn($"LogCounterRuleCache: {message}");
        }

        private async Task RefreshAsync(CancellationToken token)
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString, log);

            var repository = new OpenTelemetryLogCounterRepository(dbContext);

            var rows = await repository.GetAllActiveAsync(token).ConfigureAwait(false);

            var definitions = new List<(string Name, string Pattern)>(rows.Count);

            foreach (var row in rows)
            {
                definitions.Add((row.Name, row.Regex));
            }

            ApplyRules(definitions);
        }
    }
}

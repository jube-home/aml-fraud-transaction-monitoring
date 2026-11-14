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

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Jube.Data.Context;
using Jube.Data.Helpers;
using Jube.Data.Repository;

namespace Jube.Monitoring
{
    public sealed class LogCounterMatcher(Meter meter)
    {
        private readonly ConcurrentDictionary<string, Counter<long>> counters = new(StringComparer.Ordinal);
        private volatile HashSet<string> excludedNames = [];
        private readonly LogCounterRuleSet ruleSet = new();

        public async Task RefreshAsync(DbContext dbContext, CancellationToken token)
        {
            var logCounterRepository = new OpenTelemetryLogCounterRepository(dbContext);
            var excludeRepository = new OpenTelemetryExcludeRepository(dbContext);

            var rows = await logCounterRepository.GetAllActiveAsync(token).ConfigureAwait(false);

            ruleSet.Apply(rows.Select(r => (r.Name, r.Regex)),
                message => Console.WriteLine($"Jube.Monitoring LogCounterMatcher: {message}"));

            var excludeRows = await excludeRepository.GetAllActiveAsync(token).ConfigureAwait(false);
            var refreshedExcludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in excludeRows)
            {
                if (!string.IsNullOrEmpty(row.Name))
                {
                    refreshedExcludes.Add(row.Name);
                }
            }

            excludedNames = refreshedExcludes;
        }

        public void CheckAndIncrement(string? text)
        {
            ruleSet.Match(text!, name => excludedNames.Contains(name), Increment,
                message => Console.WriteLine($"Jube.Monitoring LogCounterMatcher: {message}"));
        }

        private void Increment(string name)
        {
            counters.GetOrAdd(name, static (key, meterState) => meterState.CreateCounter<long>(key), meter).Add(1);
        }
    }
}

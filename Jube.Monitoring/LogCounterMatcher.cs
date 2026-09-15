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
using System.Text.RegularExpressions;
using Jube.Data.Context;
using Jube.Data.Repository;

namespace Jube.Monitoring
{
    public sealed class LogCounterMatcher(Meter meter)
    {
        private static readonly TimeSpan regexMatchTimeout = TimeSpan.FromSeconds(1);

        private readonly ConcurrentDictionary<string, Counter<long>> counters = new(StringComparer.Ordinal);
        private HashSet<string> excludedNames = [];
        private (string Name, Regex Regex)[] rules = [];

        public async Task RefreshAsync(DbContext dbContext, CancellationToken token)
        {
            var logCounterRepository = new OpenTelemetryLogCounterRepository(dbContext);
            var excludeRepository = new OpenTelemetryExcludeRepository(dbContext);

            var rows = await logCounterRepository.GetAllActiveAsync(token).ConfigureAwait(false);
            var refreshedRules = new List<(string Name, Regex Regex)>(rows.Count);
            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.Name) || string.IsNullOrEmpty(row.Regex))
                {
                    continue;
                }

                try
                {
                    refreshedRules.Add((row.Name, new Regex(row.Regex, RegexOptions.Compiled, regexMatchTimeout)));
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(
                        // ReSharper disable once LocalizableElement
                        $"Jube.Monitoring LogCounterMatcher: rule '{row.Name}' has an invalid Regex and was skipped: {ex.Message}");
                }
            }

            rules = refreshedRules.ToArray();

            var excludeRows = await excludeRepository.GetAllActiveAsync(token).ConfigureAwait(false);
            var refreshedExcludes = new HashSet<string>(StringComparer.Ordinal);
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
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (var rule in rules)
            {
                bool isMatch;
                try
                {
                    isMatch = rule.Regex.IsMatch(text);
                }
                catch (RegexMatchTimeoutException)
                {
                    continue;
                }

                if (!isMatch || excludedNames.Contains(rule.Name))
                {
                    continue;
                }

                var counter = counters.GetOrAdd(
                    rule.Name,
                    static (name, meterState) => meterState.CreateCounter<long>(name),
                    meter
                );

                counter.Add(1);
            }
        }
    }
}
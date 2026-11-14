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
using System.Text.RegularExpressions;
using Jube.Data.Helpers.Models;

namespace Jube.Data.Helpers
{
    public sealed class LogCounterRuleSet
    {
        private volatile LogCounterRule[] rules = [];

        public int Count => rules.Length;

        public void Apply(IEnumerable<(string Name, string Pattern)> definitions, Action<string> warn)
        {
            var previous = new Dictionary<(string, string), LogCounterRule>();

            foreach (var rule in rules)
            {
                previous.TryAdd((rule.Name, rule.Pattern), rule);
            }

            var refreshed = new List<LogCounterRule>();

            foreach (var (name, pattern) in definitions)
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pattern))
                {
                    continue;
                }

                if (previous.TryGetValue((name, pattern), out var existing))
                {
                    existing.Resume();
                    refreshed.Add(existing);

                    continue;
                }

                try
                {
                    refreshed.Add(new LogCounterRule(name, pattern, LogCounterRegex.Create(pattern)));
                }
                catch (ArgumentException ex)
                {
                    warn($"rule '{name}' has an invalid Regex and was skipped: {ex.Message}");
                }
            }

            rules = [.. refreshed];
        }

        public void Match(string text, Func<string, bool> isSkipped, Action<string> onMatch, Action<string> warn)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            foreach (var rule in rules)
            {
                if (rule.IsSuspended || isSkipped(rule.Name))
                {
                    continue;
                }

                bool isMatch;

                try
                {
                    isMatch = rule.Regex.IsMatch(text);
                    rule.RecordCompletion();
                }
                catch (RegexMatchTimeoutException)
                {
                    var timeouts = rule.RecordTimeout();

                    warn(timeouts >= LogCounterRule.MaxConsecutiveTimeouts
                        ? $"rule '{rule.Name}' timed out {timeouts} times in a row and is suspended until the next refresh."
                        : $"rule '{rule.Name}' timed out matching a log line and was skipped.");

                    continue;
                }

                if (isMatch)
                {
                    onMatch(rule.Name);
                }
            }
        }
    }
}

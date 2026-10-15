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

using System.Collections.Concurrent;
using Jube.AskJooby.Observability.Interfaces;

namespace Jube.AskJooby.Observability
{
    public sealed class RepeatedToolLedger : IRepeatedToolLedger
    {
        private static readonly string[] repeatable =
        [
            "ListQueryableTables", "SearchRag", "InspectDataSchema", "PreviewDataRows",
            "QueryCachedData", "ValidateSqlQuery", "ExplainSqlQuery", "InspectTableStatistics",
            "AnalyseQueryHistory", "SearchCompactedHistory", "GetDemoDataset", "Recall",
            "FindGraphWorkflow", "GetGraphWorkflow"
        ];

        private readonly ConcurrentDictionary<string, object?> answers = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, byte> narrated = new(StringComparer.Ordinal);
        private int served;
        public int Served => Volatile.Read(ref served);

        public bool CanRepeat(string toolName)
        {
            return Array.IndexOf(repeatable, toolName) >= 0
                   || toolName.EndsWith("FilterFields", StringComparison.Ordinal);
        }

        public bool TryRecall(string toolName, string argumentsJson, out object? previous)
        {
            if (!CanRepeat(toolName))
            {
                previous = null;

                return false;
            }

            if (!answers.TryGetValue(Key(toolName, argumentsJson), out previous))
            {
                return false;
            }

            Interlocked.Increment(ref served);

            return true;
        }

        public bool ClaimNarration(string toolName, string argumentsJson)
        {
            return !CanRepeat(toolName) || narrated.TryAdd(Key(toolName, argumentsJson), 0);
        }

        public void Remember(string toolName, string argumentsJson, object? result)
        {
            if (CanRepeat(toolName))
            {
                answers[Key(toolName, argumentsJson)] = result;
            }
        }

        private static string Key(string toolName, string argumentsJson)
        {
            return toolName + "\u0000" + argumentsJson;
        }
    }
}
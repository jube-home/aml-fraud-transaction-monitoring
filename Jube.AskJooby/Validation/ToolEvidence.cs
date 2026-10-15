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

using Jube.AskJooby.Validation.Models;

namespace Jube.AskJooby.Validation
{
    public static class ToolEvidence
    {
        private static readonly HashSet<string> excludedToolNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "SearchRag", "AskClarifyingQuestion", "WritePlan", "SpawnAgent", "SpawnAgents", "HandBack", "Remember"
        };

        public static string Compact(
            IEnumerable<ToolCallEvidence> calls, int perItemCharacters = 400, int budgetCharacters = 4000)
        {
            var lines = new List<string>();
            var used = 0;

            foreach (var call in calls)
            {
                if (excludedToolNames.Contains(call.ToolName))
                {
                    continue;
                }

                var arguments = Truncate(call.Arguments, perItemCharacters);
                var result = Truncate(call.Result, perItemCharacters);

                var line = string.IsNullOrEmpty(arguments)
                    ? $"{call.ToolName}() -> {result}"
                    : $"{call.ToolName}({arguments}) -> {result}";

                if (budgetCharacters > 0 && used + line.Length > budgetCharacters && lines.Count > 0)
                {
                    break;
                }

                lines.Add(line);
                used += line.Length;
            }

            return string.Join("\n", lines);
        }

        private static string Truncate(string? text, int maxCharacters)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var trimmed = text.Trim();

            return trimmed.Length <= maxCharacters ? trimmed : trimmed[..maxCharacters] + "…";
        }
    }
}
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

using Jube.AskJooby.Security.Models;

namespace Jube.AskJooby.Observability
{
    public sealed record AgentGuardRejection(
        string Phase,
        string PipelineName,
        string? RuleName,
        string? Reason,
        string Action)
    {
        public static AgentGuardRejection? ForWithheldContent(string pipelineName, string singularNoun,
            string pluralNoun, IReadOnlyList<AskJoobyGuardFinding> withheld)
        {
            if (withheld.Count == 0)
            {
                return null;
            }

            var noun = withheld.Count == 1 ? singularNoun : pluralNoun;

            var byRule = withheld
                .GroupBy(finding => finding.RuleName ?? "agentguard-rule")
                .OrderByDescending(group => group.Count())
                .ToList();

            var ruleName = string.Join(", ", byRule.Select(group => group.Key));

            var reason = byRule.Count == 1
                ? $"{withheld.Count} {noun} withheld: {byRule[0].First().Reason ?? byRule[0].Key}"
                : $"{withheld.Count} {noun} withheld across {byRule.Count} distinct rules -- "
                  + string.Join("; ", byRule.Select(group => $"{group.Key} x{group.Count()}"));

            return new AgentGuardRejection(withheld[0].Phase, pipelineName, ruleName, reason, "Withheld");
        }
    }
}
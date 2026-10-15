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

using System.Text;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.Agents.Models;

namespace Jube.AskJooby.Agents.Basic
{
    public static class AgentGroundingText
    {
        public static IReadOnlyDictionary<string, string> Collect(IEnumerable<string> agentTypes)
        {
            var grounding = new Dictionary<string, string>(StringComparer.Ordinal);

            var args = new AgentToolFactoryArgs(
                null!, null!, null!, null!, 0, new RunContext(null, null, null, _ => true));

            foreach (var agentType in agentTypes.Where(t => !string.IsNullOrWhiteSpace(t))
                         .Distinct(StringComparer.Ordinal))
            {
                if (!AgentDefinitions.All.TryGetValue(agentType, out var definition))
                {
                    continue;
                }

                grounding[definition.DisplayName ?? agentType] = BuildEntry(definition, args);
            }

            return grounding;
        }

        private static string BuildEntry(AgentDefinition definition, AgentToolFactoryArgs args)
        {
            var sb = new StringBuilder();
            sb.AppendLine(definition.Instructions);

            IReadOnlyList<AgentToolDefinition> tools;

            try
            {
                tools = definition.ToolsFactory(args);
            }
            catch (Exception)
            {
                tools = [];
            }

            if (tools.Count == 0)
            {
                return sb.ToString();
            }

            sb.AppendLine();
            sb.AppendLine("Tools available to this agent:");

            foreach (var function in tools.Select(t => t.Function).DistinctBy(f => f.Name))
            {
                sb.AppendLine($"- {function.Name}: {function.Description}");
            }

            return sb.ToString();
        }
    }
}
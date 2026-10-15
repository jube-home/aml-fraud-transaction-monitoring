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

namespace Jube.AskJooby.Security
{
    public static class AgentGuardToolProvenance
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, ToolArgumentProvenance>>
            argumentsByTool = new Dictionary<string, IReadOnlyDictionary<string, ToolArgumentProvenance>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["RunSqlQuery"] = SingleArgument("sql", ToolArgumentProvenance.AlreadyValidatedElsewhere),
                ["ValidateRScript"] = SingleArgument("script", ToolArgumentProvenance.AlreadyValidatedElsewhere),
                ["RunScript"] = SingleArgument("script", ToolArgumentProvenance.AlreadyValidatedElsewhere),
                ["SpawnAgent"] = SingleArgument("task", ToolArgumentProvenance.NaturalLanguage),
                ["SpawnAgents"] = SingleArgument("subTasks", ToolArgumentProvenance.NaturalLanguage),
                ["SearchRag"] = SingleArgument("query", ToolArgumentProvenance.NaturalLanguage),
                ["SearchWeb"] = SingleArgument("query", ToolArgumentProvenance.NaturalLanguage)
            };

        private static readonly IReadOnlyDictionary<string, ToolResultProvenance> resultsByTool =
            new Dictionary<string, ToolResultProvenance>(StringComparer.OrdinalIgnoreCase)
            {
                ["AskClarifyingQuestion"] = ToolResultProvenance.PlatformGenerated,
                ["SearchRag"] = ToolResultProvenance.PreScreened
            };

        public static IDictionary<string, ISet<string>> AllowedCallArguments()
        {
            return argumentsByTool.ToDictionary(
                tool => tool.Key,
                tool => (ISet<string>)new HashSet<string>(tool.Value.Keys, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        }

        public static ISet<string> SkippedResultTools()
        {
            return new HashSet<string>(resultsByTool.Keys, StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<string, ToolArgumentProvenance> SingleArgument(
            string argumentName, ToolArgumentProvenance provenance)
        {
            return new Dictionary<string, ToolArgumentProvenance>(StringComparer.OrdinalIgnoreCase)
            {
                [argumentName] = provenance
            };
        }
    }
}
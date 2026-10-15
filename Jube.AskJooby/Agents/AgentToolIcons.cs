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
using Jube.AskJooby.Agents.Models;

namespace Jube.AskJooby.Agents
{
    public static class AgentToolIcons
    {
        public const AgentToolIcon Default = AgentToolIcon.Wrench;

        public static void Register(string toolName, AgentToolIcon icon)
        {
            registered[toolName] = icon;
        }

        public static AgentToolIcon Resolve(string toolName)
        {
            if (AgentToolIcons.registered.TryGetValue(toolName, out var registeredIcon))
            {
                return registeredIcon;
            }

            return AgentToolIcons.core.GetValueOrDefault(toolName, Default);
        }

        private static readonly IReadOnlyDictionary<string, AgentToolIcon> core =
            new Dictionary<string, AgentToolIcon>(StringComparer.Ordinal)
            {
                ["SearchRag"] = AgentToolIcon.BookSearch,
                ["SearchWeb"] = AgentToolIcon.Globe,
                ["SearchCompanyByName"] = AgentToolIcon.Landmark,
                ["GetCompanyDetails"] = AgentToolIcon.Landmark,
                ["SearchBeneficialOwners"] = AgentToolIcon.Group,
                ["AskClarifyingQuestion"] = AgentToolIcon.MessageCircleQuestion,
                ["FindGraphWorkflow"] = AgentToolIcon.Route,
                ["GetGraphWorkflow"] = AgentToolIcon.Workflow,
                ["GetWorkflowProgress"] = AgentToolIcon.ListChecks,
                ["RecordWorkflowStep"] = AgentToolIcon.SquareCheckBig,
                ["HandBack"] = AgentToolIcon.Undo2,
                ["Remember"] = AgentToolIcon.BookmarkPlus,
                ["Recall"] = AgentToolIcon.Bookmark,
                ["SearchHistory"] = AgentToolIcon.History,
                ["SearchCompactedHistory"] = AgentToolIcon.Archive,
                ["ListQueryableTables"] = AgentToolIcon.TableProperties,
                ["RunSqlQuery"] = AgentToolIcon.Database,
                ["ValidateSqlQuery"] = AgentToolIcon.ShieldCheck,
                ["ExplainSqlQuery"] = AgentToolIcon.FileSearch,
                ["InspectTableStatistics"] = AgentToolIcon.ChartColumn,
                ["AnalyseQueryHistory"] = AgentToolIcon.Timer,
                ["InspectDataSchema"] = AgentToolIcon.Columns3,
                ["PreviewDataRows"] = AgentToolIcon.Eye,
                ["QueryCachedData"] = AgentToolIcon.Filter,
                ["ExportCachedTableToCsv"] = AgentToolIcon.FileDown,
                ["GetDemoDataset"] = AgentToolIcon.FlaskConical,
                ["RenderChart"] = AgentToolIcon.ChartLine,
                ["SpawnAgent"] = AgentToolIcon.UsersRound,
                ["SpawnAgents"] = AgentToolIcon.UsersRound,
                ["WritePlan"] = AgentToolIcon.ClipboardList
            };

        private static readonly ConcurrentDictionary<string, AgentToolIcon> registered = new(StringComparer.Ordinal);
    }
}
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Models;
using Jube.Service.Agent;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.PageAgentToolBinder;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class AgentToolIconsTests
    {
        private static readonly string[] coreTools =
        [
            "SearchRag", "SearchWeb", "AskClarifyingQuestion", "FindGraphWorkflow", "GetGraphWorkflow",
            "GetWorkflowProgress",
            "RecordWorkflowStep", "HandBack", "Remember", "Recall", "SearchHistory", "SearchCompactedHistory",
            "ListQueryableTables", "RunSqlQuery", "ValidateSqlQuery", "ExplainSqlQuery", "InspectTableStatistics",
            "AnalyseQueryHistory", "InspectDataSchema", "PreviewDataRows", "QueryCachedData", "ExportCachedTableToCsv",
            "GetDemoDataset", "RenderChart", "SpawnAgent", "SpawnAgents", "WritePlan"
        ];

        [Fact]
        public void EveryCoreToolHasItsOwnIcon()
        {
            foreach (var name in coreTools)
            {
                AgentToolIcons.Resolve(name).Should().NotBe(AgentToolIcons.Default, name);
            }
        }

        [Fact]
        public void EveryAnalyticsToolResolvesToItsGroupIcon()
        {
            var names = AdvancedAnalyticsAgentToolBinder.Descriptors
                .Select(descriptor => descriptor.ToolName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            names.Should().NotBeEmpty();

            foreach (var name in names)
            {
                AgentToolIcons.Resolve(name!).Should().NotBe(AgentToolIcons.Default, name);
            }
        }

        [Fact]
        public void EveryServiceOperationResolvesToADomainIcon()
        {
            var names = ServiceOperationIndex.All.Keys.ToList();

            names.Should().NotBeEmpty();

            names.Where(name => AgentToolIcons.Resolve(name) == AgentToolIcons.Default)
                .Should().BeEmpty();
        }

        [Theory]
        [InlineData("TreeChildrenUserRegistry", AgentToolIcon.KeyRound)]
        [InlineData("TreeChildrenCaseWorkflowForm", AgentToolIcon.Briefcase)]
        [InlineData("TreeChildrenInlineScript", AgentToolIcon.SlidersHorizontal)]
        [InlineData("TreeChildrenExhaustive", AgentToolIcon.FlaskConical)]
        public void TreeChildrenNavigationTakesTheIconOfItsEntity(string name, AgentToolIcon icon)
        {
            ServiceOperationIcons.For(name).Should().Be(icon);
        }

        [Fact]
        public void EveryIconInTheEnumHasABrowserBody()
        {
            var script = ReadRepositoryFile("Jube.App/wwwroot/js/AskJooby/AskJoobyToolIcons.js");

            Enum.GetValues<AgentToolIcon>()
                .Where(icon => !script.Contains($"    {(int)icon}: \"", StringComparison.Ordinal))
                .Should().BeEmpty();
        }

        [Fact]
        public void EveryFunctionToolGroupHasItsOwnIcon()
        {
            var groups = typeof(FunctionToolGroupAttribute).Assembly.GetTypes()
                .Select(type => type.GetCustomAttribute<FunctionToolGroupAttribute>())
                .Where(attribute => attribute is not null)
                .ToList();

            groups.Should().NotBeEmpty();

            groups.Where(attribute => FunctionToolGroupIcons.For(attribute!.Name) == AgentToolIcons.Default)
                .Should().BeEmpty();
        }

        [Fact]
        public void StepForAToolCarriesItsIcon()
        {
            var item = AgentStreamNarrator.DescribeStart(
                new FunctionCallContent("call-icon", "RunSqlQuery", new Dictionary<string, object?>()));

            item.Should().NotBeNull();
            item!.Icon.Should().Be(AgentToolIcon.Database);
        }

        private static string ReadRepositoryFile(string relative)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.sln")))
            {
                directory = directory.Parent;
            }

            directory.Should().NotBeNull();

            return File.ReadAllText(Path.Combine(directory!.FullName, relative));
        }
    }
}
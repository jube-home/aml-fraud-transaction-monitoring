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
using System.Linq;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.PageAgentToolBinder;
using Jube.Service.Agent.PageAgentToolBinder.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.ArchiverWarning;
using Jube.Service.EntityAnalysisModelTag;
using Jube.Service.PostgresMetric;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.Service.Agent
{
    [Trait("Category", "Service")]
    public sealed class ServiceToolCatalogueTests
    {
        private static readonly string[] permissionFreeOperations =
            ["QueryBuilderOperatorsList", "VisualisationRegistryListByShowInDirectory"];

        [Fact]
        public void EveryToolNameIsUniqueAcrossTheWholeCatalogue()
        {
            var names = ServiceToolCatalogue.All.Select(tool => tool.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void EveryToolCarriesAtLeastOneRequiredPermissionUnlessItIsAPermissionFreeRead()
        {
            ServiceToolCatalogue.All.Should()
                .OnlyContain(tool => tool.RequiredPermissions.Count > 0 || tool.RequiresLandlord
                                                                        || Enumerable.Contains(
                                                                            permissionFreeOperations, tool.Name));
        }

        [Fact]
        public void EveryPermissionFreeOperationNamesAnOperationThatExistsAndIsARead()
        {
            foreach (var name in permissionFreeOperations)
            {
                var tool = ServiceToolCatalogue.All.SingleOrDefault(t => t.Name == name);

                (tool is null).Should()
                    .BeFalse(name + " is permission free but is not in the catalogue");

                tool!.Kind.Should().Be(OperationKind.Read, name + " may only be exempted if it changes nothing");

                tool.RequiredPermissions.Should().BeEmpty(
                    name + " carries a permission, so it does not need the exemption");
            }
        }

        [Fact]
        public void EveryDescriptorsRequiredPermissionsMatchesItsServicesOwnField()
        {
            var postgresMetricList = ServiceToolCatalogue.All.Single(tool => tool.Name == "PostgresMetricList");
            postgresMetricList.RequiredPermissions.Should().BeEquivalentTo(PostgresMetricService.RequiredPermissions);

            var tagCreate = ServiceToolCatalogue.All.Single(tool => tool.Name == "EntityAnalysisModelTagCreate");
            tagCreate.RequiredPermissions.Should().BeEquivalentTo(EntityAnalysisModelTagService.RequiredPermissions);

            var archiverWarningList = ServiceToolCatalogue.All.Single(tool => tool.Name == "ArchiverWarningList");
            archiverWarningList.RequiresLandlord.Should().Be(ArchiverWarningService.RequiresLandlord);
            archiverWarningList.RequiredPermissions.Should()
                .BeEquivalentTo(ArchiverWarningService.RequiredPermissions);
        }
    }

    [Trait("Category", "Service")]
    public sealed class PageAgentToolBinderTests
    {
        private static AIFunction StubFunction()
        {
            return AIFunctionFactory.Create(() => "stub", "StubTool");
        }

        private static ServiceAgentBindingContext Context(bool readOnly = false)
        {
            return new ServiceAgentBindingContext(null!, "test-user", null!, default, readOnly);
        }

        [Fact]
        public void AddBoundReadsPermissionsFromTheCatalogueNotAHandTypedCopy()
        {
            var tools = new List<AgentToolDefinition>();

            PageAgentToolBinder.AddBound(Context(), tools, "PostgresMetricList", StubFunction());

            tools.Should().ContainSingle();
            tools[0].RequiredPermissions.Should().BeEquivalentTo(PostgresMetricService.RequiredPermissions);
        }

        [Fact]
        public void AddBoundMarksAReadToolAsNotRequiringASnapshot()
        {
            var tools = new List<AgentToolDefinition>();

            PageAgentToolBinder.AddBound(Context(), tools, "PostgresMetricList", StubFunction());

            tools[0].RequiresSnapshot.Should().BeFalse();
        }

        [Fact]
        public void AddBoundMarksAWriteToolAsRequiringASnapshot()
        {
            var tools = new List<AgentToolDefinition>();

            PageAgentToolBinder.AddBound(Context(), tools, "EntityAnalysisModelTagCreate", StubFunction());

            tools[0].RequiresSnapshot.Should().BeTrue();
        }

        [Fact]
        public void AddBoundThrowsForAToolNameNotInTheCatalogue()
        {
            var tools = new List<AgentToolDefinition>();

            var act = () => PageAgentToolBinder.AddBound(Context(), tools, "NotARealTool", StubFunction());

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AddBoundSkipsAWriteToolEntirelyWhenReadOnly()
        {
            var tools = new List<AgentToolDefinition>();

            PageAgentToolBinder.AddBound(Context(true), tools, "EntityAnalysisModelTagCreate", StubFunction());

            tools.Should().BeEmpty();
        }

        [Fact]
        public void AddBoundStillBindsAReadToolWhenReadOnly()
        {
            var tools = new List<AgentToolDefinition>();

            PageAgentToolBinder.AddBound(Context(true), tools, "PostgresMetricList", StubFunction());

            tools.Should().ContainSingle();
        }
    }
}
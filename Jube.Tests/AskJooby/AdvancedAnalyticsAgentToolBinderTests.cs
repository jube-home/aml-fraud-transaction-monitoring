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

using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AdvancedAnalyticsAgentToolBinderTests
    {
        [Fact]
        public void TheCatalogueDiscoversTheGroupsTheLayerWasBuiltAround()
        {
            AdvancedAnalyticsAgentToolBinder.ComputationGroups.Should().HaveCountGreaterThan(1,
                "the analytics surface is grouped, and a single group would mean discovery found almost nothing");
        }

        [Fact]
        public void EveryDiscoveredGroupBindsAtLeastOneLiveFunction()
        {
            foreach (var group in AdvancedAnalyticsAgentToolBinder.ComputationGroups)
            {
                AdvancedAnalyticsAgentToolBinder.Bind(group).Should().NotBeEmpty(
                    $"group \"{group}\" was discovered, so it must also bind");
            }
        }

        [Fact]
        public void EveryBoundToolCarriesAPermission()
        {
            foreach (var group in AdvancedAnalyticsAgentToolBinder.ComputationGroups)
            {
                AdvancedAnalyticsAgentToolBinder.Bind(group).Should()
                    .OnlyContain(tool => tool.RequiredPermissions.Count > 0,
                        "an empty permission array means the tool is open to everyone");
            }
        }

        [Fact]
        public void EveryBoundToolIsNamedAndDescribed()
        {
            foreach (var group in AdvancedAnalyticsAgentToolBinder.ComputationGroups)
            {
                AdvancedAnalyticsAgentToolBinder.Bind(group).Should()
                    .OnlyContain(tool => !string.IsNullOrWhiteSpace(tool.Function.Name)
                                         && !string.IsNullOrWhiteSpace(tool.Function.Description),
                        "a model chooses a tool from its name and description alone");
            }
        }

        [Fact]
        public void ToolNamesAreUniqueAcrossTheWholeSurface()
        {
            var duplicates = AdvancedAnalyticsAgentToolBinder.ComputationGroups
                .SelectMany(group => AdvancedAnalyticsAgentToolBinder.Bind(group))
                .GroupBy(tool => tool.Function.Name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            duplicates.Should().BeEmpty(
                "two tools sharing a name would make one of them unreachable once both are bound");
        }

        [Fact]
        public void TheTrainingRunFamiliesAreHeldBackForTheServicePath()
        {
            AdvancedAnalyticsAgentToolBinder.ServiceConstructedGroups.Should().NotBeEmpty(
                "the training-run families take constructor dependencies and cannot bind as pure computation");

            AdvancedAnalyticsAgentToolBinder.ServiceConstructedGroups
                .Should().NotIntersectWith(AdvancedAnalyticsAgentToolBinder.ComputationGroups,
                    "a group is either a pure function or an operation against live state, never both");
        }

        [Fact]
        public async Task ABoundToolActuallyComputesWhenInvokedAsync()
        {
            var mean = AdvancedAnalyticsAgentToolBinder.Bind("DescriptiveStatistics")
                .Single(tool => tool.Function.Name == "Mean");

            var result = await mean.Function.InvokeAsync(
                new AIFunctionArguments { ["values"] = new[] { 2.0, 4.0, 6.0 } }, CancellationToken.None);

            JsonSerializer.Serialize(result).Should().Contain("4",
                "the mean of 2, 4 and 6 is 4 -- this proves the binding reaches real library code");
        }
    }
}
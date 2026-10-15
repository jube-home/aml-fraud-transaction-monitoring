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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation;
using Jube.Test.Security.PenTest;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class AdvancedAnalyticsTrainingAgentCoverageTests
    {
        private const int MaximumToolsPerAgent = 10;

        private static readonly IReadOnlyDictionary<string, string> deliberatelyUnbound =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["GetTrainingRunStatus"] = "Logistic-typed; the generic triple covers every classifier.",
                ["CancelTrainingRun"] = "Logistic-typed; the generic triple covers every classifier.",
                ["GetTrainingRunResult"] =
                    "Types evidence as logistic only, so it would misread the other two probabilistic families.",
                ["GetClustererTrainingRunStatus"] = "KMeans-typed; the generic triple covers every clusterer.",
                ["CancelClustererTrainingRun"] = "KMeans-typed; the generic triple covers every clusterer.",
                ["GetClustererTrainingRunResult"] =
                    "Types evidence as KMeans only, so it would misread the seven other clusterer families.",
                ["GetLinearRegressionTrainingRunStatus"] = "Linear-typed; the generic triple covers every regressor.",
                ["CancelLinearRegressionTrainingRun"] = "Linear-typed; the generic triple covers every regressor.",
                ["GetLinearRegressionTrainingRunResult"] =
                    "Types evidence as linear regression only, so it would misread the six other regressor families.",
                ["GetMahalanobisAnomalyTrainingRunStatus"] =
                    "Mahalanobis-typed; the generic triple covers every anomaly detector.",
                ["CancelMahalanobisAnomalyTrainingRun"] =
                    "Mahalanobis-typed; the generic triple covers every anomaly detector.",
                ["GetMahalanobisAnomalyTrainingRunResult"] =
                    "Types evidence as Mahalanobis only, so it would misread the two other anomaly families.",
                ["GetPrincipalComponentTrainingRunStatus"] =
                    "Principal-component-typed; the generic triple covers both latent-structure families.",
                ["CancelPrincipalComponentTrainingRun"] =
                    "Principal-component-typed; the generic triple covers both latent-structure families.",
                ["GetPrincipalComponentTrainingRunResult"] =
                    "Types evidence as principal components only, so it would misread a density run.",
                ["GetGaussianMixtureDensityTrainingRunStatus"] =
                    "Density-typed; the generic triple covers both latent-structure families.",
                ["CancelGaussianMixtureDensityTrainingRun"] =
                    "Density-typed; the generic triple covers both latent-structure families.",
                ["GetGaussianMixtureDensityTrainingRunResult"] =
                    "Types evidence as a density only, so it would misread a principal-component run."
            };

        private static readonly Regex declaredKind =
            new(@"AdvancedAnalyticsKind\.(?<kind>[A-Za-z]+)", RegexOptions.Compiled);

        private static IEnumerable<string> ClaimedTools()
        {
            return AdvancedAnalyticsTrainingAgentToolNames.All.SelectMany(pair => pair.Value)
                .Concat(AdvancedAnalyticsModelEvaluationAgentToolNames.All.SelectMany(pair => pair.Value));
        }

        private static IReadOnlyList<AdvancedAnalyticsToolDescriptor> ServiceConstructed()
        {
            return
            [
                .. AdvancedAnalyticsAgentToolBinder.Descriptors
                    .Where(descriptor => !AdvancedAnalyticsAgentToolBinder.IsPureComputation(descriptor.GroupType)
                                         && descriptor.GroupType != typeof(ModelSelectionTools))
            ];
        }

        [Fact]
        public void EveryTrainingToolIsEitherOwnedByOneAgentOrUnboundForAStatedReason()
        {
            var claimed = ClaimedTools().ToHashSet(StringComparer.Ordinal);

            var unaccounted = ServiceConstructed()
                .Select(descriptor => descriptor.ToolName!)
                .Where(name => !claimed.Contains(name) && !deliberatelyUnbound.ContainsKey(name))
                .OrderBy(name => name, StringComparer.Ordinal).ToList();

            unaccounted.Should().BeEmpty(
                "a training tool nobody holds can never be called, so it must either be given to an "
                + "agent or recorded here with the reason it is not: " + string.Join(", ", unaccounted));

            deliberatelyUnbound.Keys.Except(ServiceConstructed().Select(d => d.ToolName!))
                .Should().BeEmpty("an exclusion for a tool that no longer exists hides the next one");
        }

        [Fact]
        public void NoTrainingToolIsOwnedByTwoAgentsExceptTheRunControlEveryOneOfThemNeeds()
        {
            var shared = new[]
            {
                "GetAdvancedAnalyticsTrainingRunStatus", "CancelAdvancedAnalyticsTrainingRun",
                "GetAdvancedAnalyticsTrainingRunResult"
            };

            var duplicated = ClaimedTools().GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1).Select(group => group.Key)
                .Except(shared, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal).ToList();

            duplicated.Should().BeEmpty(
                "a family held by two agents makes it ambiguous which of them a caller should ask: "
                + string.Join(", ", duplicated));

            foreach (var (agentType, toolNames) in AdvancedAnalyticsTrainingAgentToolNames.All)
            {
                toolNames.Should().Contain(shared, agentType
                                                   + " cannot follow a run it starts without the run control triple");
            }
        }

        [Fact]
        public void EveryClaimedToolActuallyExistsInTheAnalyticsCatalogue()
        {
            var known = AdvancedAnalyticsAgentToolBinder.Descriptors
                .Select(descriptor => descriptor.ToolName).ToHashSet(StringComparer.Ordinal);

            var phantom = ClaimedTools().Distinct(StringComparer.Ordinal)
                .Where(name => !known.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal).ToList();

            phantom.Should().BeEmpty(
                "an agent granted a tool the catalogue does not carry throws at binding: "
                + string.Join(", ", phantom));
        }

        [Fact]
        public void NoTrainingAgentHoldsMoreThanTheHouseLimit()
        {
            var oversized = AdvancedAnalyticsTrainingAgentToolNames.All
                .Where(pair => pair.Value.Count > MaximumToolsPerAgent)
                .Select(pair => $"{pair.Key} ({pair.Value.Count})")
                .OrderBy(name => name, StringComparer.Ordinal).ToList();

            oversized.Should().BeEmpty(
                $"a training agent over {MaximumToolsPerAgent} tools must be divided on an academic line: "
                + string.Join(", ", oversized));
        }

        [Fact]
        public void EveryTrainingAgentIsRegisteredAsALeafUnderModelRisk()
        {
            foreach (var agentType in AdvancedAnalyticsTrainingAgentToolNames.All.Keys)
            {
                AgentDefinitions.All.Should().ContainKey(agentType,
                    "a tool map entry with no agent definition binds tools nobody can reach");

                AgentDefinitions.All[agentType].AllowedDelegates.Should().BeSubsetOf(["web-researcher"],
                    agentType + " trains and does not route, except to bounce an open-internet question to "
                              + "the shared web research specialist");

                AgentDefinitions.HierarchyDomainOf.Should().ContainKey(agentType);
                AgentDefinitions.HierarchyDomainOf[agentType].Should().Be("model-risk");
            }
        }

        [Fact]
        public void EveryTrainingAgentIsReachableFromTheRootWithinTheDelegationDepthBudget()
        {
            var depth = new Dictionary<string, int> { ["general"] = 0 };
            var queue = new Queue<string>(["general"]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var child in AgentDefinitions.All[current].AllowedDelegates ?? [])
                {
                    if (depth.TryAdd(child, depth[current] + 1))
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            foreach (var agentType in AdvancedAnalyticsTrainingAgentToolNames.All.Keys)
            {
                depth.Should().ContainKey(agentType, agentType + " can never be delegated to");

                depth[agentType].Should().BeLessThanOrEqualTo(3,
                    "RunContext.MaxDepth is 3, so a deeper leaf could never be spawned");
            }
        }

        [Fact]
        public void EveryTrainingAgentDeclaresThePermissionsItsOwnToolsRequire()
        {
            var byName = AdvancedAnalyticsAgentToolBinder.Descriptors
                .Where(descriptor => !string.IsNullOrWhiteSpace(descriptor.ToolName))
                .GroupBy(descriptor => descriptor.ToolName!, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            foreach (var (agentType, toolNames) in AdvancedAnalyticsTrainingAgentToolNames.All)
            {
                var expected = toolNames
                    .SelectMany(name => AdvancedAnalyticsAgentToolBinder
                        .PermissionIdsFor(byName[name].Permissions))
                    .Distinct().OrderBy(id => id).ToList();

                AgentDefinitions.All[agentType].RequiredPermissions.Should()
                    .BeEquivalentTo(expected, agentType);
            }
        }

        [Fact]
        public void EveryToolClassResolvesToATrainingServiceTheBinderCanConstruct()
        {
            foreach (var groupType in ServiceConstructed().Select(d => d.GroupType).Distinct())
            {
                AdvancedAnalyticsTrainingToolBinder
                    .TryResolveChain(groupType, out var chain, out var serviceType)
                    .Should().BeTrue(groupType.Name
                                     + " exposes no path from the tool class to a service with a CreateAsync factory");

                chain.Should().NotBeEmpty();
                serviceType.Name.Should().EndWith("Service");
            }
        }

        [Fact]
        public void NoAgentMixesKindsBeyondTheOneThatDeliberatelySpansTwo()
        {
            var kindOfService = KindsDeclaredByTrainingServices();

            foreach (var (agentType, toolNames) in AdvancedAnalyticsTrainingAgentToolNames.All)
            {
                var declared = AdvancedAnalyticsTrainingAgentToolNames.KindsOf[agentType];

                declared.Should().OnlyContain(kind => Enum.IsDefined(typeof(AdvancedAnalyticsKind), kind),
                    agentType + " names a kind the enum does not carry");

                foreach (var toolName in toolNames.Where(name => name.StartsWith("Submit", StringComparison.Ordinal)))
                {
                    var serviceType = ServiceBehind(toolName);

                    kindOfService.Should().ContainKey(serviceType,
                        serviceType + " declares no AdvancedAnalyticsKind of its own");

                    declared.Should().Contain(kindOfService[serviceType],
                        $"{agentType} holds {toolName}, whose service trains a "
                        + $"{kindOfService[serviceType]} it does not declare");
                }
            }

            AdvancedAnalyticsTrainingAgentToolNames.KindsOf
                .Count(pair => pair.Value.Count > 1).Should().Be(1,
                    "only latent structure spans two kinds, and only because each has a single family");
        }

        private static string ServiceBehind(string toolName)
        {
            var descriptor = AdvancedAnalyticsAgentToolBinder.Descriptors
                .First(candidate => string.Equals(candidate.ToolName, toolName, StringComparison.Ordinal));

            AdvancedAnalyticsTrainingToolBinder
                .TryResolveChain(descriptor.GroupType, out _, out var serviceType).Should().BeTrue();

            return serviceType.Name;
        }

        private static IReadOnlyDictionary<string, string> KindsDeclaredByTrainingServices()
        {
            var directory = Path.Combine(PenTestPaths.RepositoryRoot, "Jube.Service", "Repository",
                "AdvancedAnalyticsTrainingRun");

            var kinds = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var file in Directory.EnumerateFiles(directory, "*Service.cs"))
            {
                var match = declaredKind.Match(File.ReadAllText(file));

                if (match.Success)
                {
                    kinds[Path.GetFileNameWithoutExtension(file)] = match.Groups["kind"].Value;
                }
            }

            return kinds;
        }
    }
}
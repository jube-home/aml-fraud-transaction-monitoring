/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
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
using System.Text;
using System.Text.RegularExpressions;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Test.Security.PenTest;

namespace Jube.Test.Service.Agent
{
    internal static class AgentDocuments
    {
        internal const string WriteFlag = "**Write — confirmation required**";

        private static readonly IReadOnlyList<(string Name, OperationKind Kind, bool Idempotent, bool Destructive)>
            trainingToolShapes =
            [
                ("SubmitDecisionTreeClassifierTraining", OperationKind.Write, false, false),
                ("SubmitRandomForestClassifierTraining", OperationKind.Write, false, false),
                ("SubmitBaggingClassifierTraining", OperationKind.Write, false, false),
                ("SubmitAdaBoostClassifierTraining", OperationKind.Write, false, false),
                ("SubmitGradientBoostingClassifierTraining", OperationKind.Write, false, false),
                ("SubmitLogisticRegressionTraining", OperationKind.Write, false, false),
                ("SubmitNaiveBayesClassifierTraining", OperationKind.Write, false, false),
                ("SubmitTreeAugmentedNaiveBayesClassifierTraining", OperationKind.Write, false, false),
                ("SubmitLinearSupportVectorClassifierTraining", OperationKind.Write, false, false),
                ("SubmitKernelSupportVectorClassifierTraining", OperationKind.Write, false, false),
                ("SubmitKNearestNeighboursClassifierTraining", OperationKind.Write, false, false),
                ("SubmitNeuralNetworkClassifierTraining", OperationKind.Write, false, false),
                ("SubmitKMeansClustererTraining", OperationKind.Write, false, false),
                ("SubmitMiniBatchKMeansClustererTraining", OperationKind.Write, false, false),
                ("SubmitBalancedKMeansClustererTraining", OperationKind.Write, false, false),
                ("SubmitKMedoidsClustererTraining", OperationKind.Write, false, false),
                ("SubmitKModesClustererTraining", OperationKind.Write, false, false),
                ("SubmitBinarySplitClustererTraining", OperationKind.Write, false, false),
                ("SubmitMeanShiftClustererTraining", OperationKind.Write, false, false),
                ("SubmitGaussianMixtureClustererTraining", OperationKind.Write, false, false),
                ("SubmitLinearRegressionTraining", OperationKind.Write, false, false),
                ("SubmitKernelRidgeRegressorTraining", OperationKind.Write, false, false),
                ("SubmitDecisionTreeRegressorTraining", OperationKind.Write, false, false),
                ("SubmitRandomForestRegressorTraining", OperationKind.Write, false, false),
                ("SubmitGradientBoostingRegressorTraining", OperationKind.Write, false, false),
                ("SubmitKNearestNeighboursRegressorTraining", OperationKind.Write, false, false),
                ("SubmitNeuralNetworkRegressorTraining", OperationKind.Write, false, false),
                ("SubmitIsolationForestAnomalyTraining", OperationKind.Write, false, false),
                ("SubmitMahalanobisAnomalyTraining", OperationKind.Write, false, false),
                ("SubmitOneClassSupportVectorAnomalyTraining", OperationKind.Write, false, false),
                ("SubmitPrincipalComponentTraining", OperationKind.Write, false, false),
                ("SubmitGaussianMixtureDensityTraining", OperationKind.Write, false, false),
                ("GetAdvancedAnalyticsTrainingRunStatus", OperationKind.Read, true, false),
                ("CancelAdvancedAnalyticsTrainingRun", OperationKind.Write, true, false),
                ("GetAdvancedAnalyticsTrainingRunResult", OperationKind.Read, true, false)
            ];

        internal static readonly IReadOnlyList<string> sharedAcrossAgentsByDesign =
        [
            "GetAdvancedAnalyticsTrainingRunStatus", "CancelAdvancedAnalyticsTrainingRun",
            "GetAdvancedAnalyticsTrainingRunResult"
        ];

        internal static IReadOnlyList<ServiceToolDescriptor> KnownTools { get; } =
            [.. ServiceToolCatalogue.All, .. BuildTrainingToolDescriptors()];

        private static IReadOnlyList<ServiceToolDescriptor> BuildTrainingToolDescriptors()
        {
            return BuildAdvancedAnalyticsToolDescriptors(trainingToolShapes);
        }

        private static IReadOnlyList<ServiceToolDescriptor> BuildAdvancedAnalyticsToolDescriptors(
            IReadOnlyList<(string Name, OperationKind Kind, bool Idempotent, bool Destructive)> shapes)
        {
            var byName = AdvancedAnalyticsAgentToolBinder.Descriptors
                .Where(descriptor => !string.IsNullOrWhiteSpace(descriptor.ToolName))
                .ToDictionary(descriptor => descriptor.ToolName!, StringComparer.Ordinal);

            return
            [
                .. shapes.Select(shape =>
                {
                    var descriptor = byName[shape.Name];

                    return new ServiceToolDescriptor(shape.Name, shape.Kind, shape.Idempotent, shape.Destructive,
                        descriptor.Description!,
                        AdvancedAnalyticsAgentToolBinder.PermissionIdsFor(descriptor.Permissions));
                })
            ];
        }

        private static readonly Regex leafHeading =
            new(@"^#### (?<name>.+?) \(`(?<slug>[a-z0-9-]+)`\)\s*$", RegexOptions.Compiled);

        private static readonly Regex inventorySection =
            new(@"^### (?<tool>[A-Za-z0-9_]+)\s*$", RegexOptions.Compiled);

        private static readonly Regex inventoryRow =
            new(
                @"^\|\s*`(?<tool>[A-Za-z0-9_]+)`\s*\|\s*(?<kind>\w+)\s*\|\s*`(?<slug>[a-z0-9-]+|—)`\s*\|\s*(?<access>.+?)\s*\|\s*$",
                RegexOptions.Compiled);

        private static readonly Lazy<IReadOnlyList<HierarchyAgent>> agentsLazy = new(ParseHierarchy);
        private static readonly Lazy<IReadOnlyList<InventoryEntry>> inventoryLazy = new(ParseInventory);
        internal static string Directory => Path.Combine(PenTestPaths.RepositoryRoot, "docs", "AgentHierarchy");
        internal static string HierarchyPath => Path.Combine(Directory, "index.md");
        internal static string InventoryPath => Path.Combine(Directory, "tool-inventory.md");
        internal static IReadOnlyList<HierarchyAgent> Agents => agentsLazy.Value;
        internal static IReadOnlyList<InventoryEntry> Inventory => inventoryLazy.Value;

        internal static Regex ToolBulletPattern { get; } =
            new(@"^ {2,4}\* `(?<tool>[A-Za-z0-9_]+)`:", RegexOptions.Compiled);

        private static bool EndsBulletOrSection(string line)
        {
            return string.IsNullOrWhiteSpace(line) || ToolBulletPattern.IsMatch(line)
                                                   || line.StartsWith("#### ", StringComparison.Ordinal)
                                                   || line.StartsWith("### ", StringComparison.Ordinal)
                                                   || line.StartsWith("## ", StringComparison.Ordinal)
                                                   || line.StartsWith("- ", StringComparison.Ordinal);
        }

        private static string AccumulateBulletText(string[] lines, int start, out int next)
        {
            var text = new StringBuilder(lines[start]);
            var j = start + 1;

            while (j < lines.Length && !EndsBulletOrSection(lines[j]))
            {
                text.Append(' ').Append(lines[j].Trim());
                j++;
            }

            next = j;
            return text.ToString();
        }

        internal static IEnumerable<(string Tool, string FullText)> ToolBulletsWithFullText(string[] lines)
        {
            var i = 0;

            while (i < lines.Length)
            {
                var match = ToolBulletPattern.Match(lines[i]);

                if (!match.Success)
                {
                    i++;
                    continue;
                }

                var text = AccumulateBulletText(lines, i, out var next);

                yield return (match.Groups["tool"].Value, text);
                i = next;
            }
        }

        private static IReadOnlyList<HierarchyAgent> ParseHierarchy()
        {
            var lines = File.ReadAllLines(HierarchyPath);
            var agents = new List<HierarchyAgent>();
            HierarchyAgent? current = null;
            var i = 0;

            while (i < lines.Length)
            {
                var line = lines[i];
                var heading = leafHeading.Match(line);

                if (heading.Success)
                {
                    current = new HierarchyAgent(heading.Groups["name"].Value, heading.Groups["slug"].Value);
                    agents.Add(current);
                    i++;
                    continue;
                }

                if (line.StartsWith("#### ", StringComparison.Ordinal) ||
                    line.StartsWith("## ", StringComparison.Ordinal))
                {
                    current = null;
                    i++;
                    continue;
                }

                var tool = ToolBulletPattern.Match(line);

                if (tool.Success && current != null)
                {
                    var text = AccumulateBulletText(lines, i, out var next);

                    current.Tools.Add(new AssignedTool(tool.Groups["tool"].Value, text.Contains(WriteFlag)));
                    i = next;
                    continue;
                }

                i++;
            }

            return agents;
        }

        private static IReadOnlyList<InventoryEntry> ParseInventory()
        {
            var rows = new Dictionary<string, InventoryRowValues>();
            var sections = new List<string>();

            foreach (var line in File.ReadLines(InventoryPath))
            {
                var row = inventoryRow.Match(line);

                if (row.Success)
                {
                    rows[row.Groups["tool"].Value] = new InventoryRowValues(row.Groups["kind"].Value,
                        row.Groups["slug"].Value, row.Groups["access"].Value.Trim());

                    continue;
                }

                var section = inventorySection.Match(line);

                if (section.Success)
                {
                    sections.Add(section.Groups["tool"].Value);
                }
            }

            var entries = new List<InventoryEntry>();

            foreach (var tool in sections)
            {
                rows.TryGetValue(tool, out var values);
                entries.Add(new InventoryEntry(tool, values));
            }

            foreach (var tool in rows.Keys)
            {
                if (!sections.Contains(tool))
                {
                    entries.Add(new InventoryEntry(tool, null));
                }
            }

            return entries;
        }
    }
}
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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Preservation.Models;
using MessagePack;
using Xunit;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Unit")]
    public sealed class PreservationModelTreeCoverageTests
    {
        private static readonly string[] treePrefixes =
            ["EntityAnalysisModel", "CaseWorkflow", "VisualisationRegistry", "RoleRegistry"];

        private static readonly Dictionary<string, string> excluded = new()
        {
            ["CaseWorkflowDispatch"] =
                "Case data. The audit of calls fired against live cases, not part of the tenant's definition.",
            ["CaseWorkflowFormEntry"] =
                "Case data. Entered by investigators against live cases, not part of the tenant's definition.",
            ["CaseWorkflowFormEntryValue"] =
                "Case data. Entered by investigators against live cases, not part of the tenant's definition.",
            ["EntityAnalysisModelActivationRuleCounterHistory"] =
                "Runtime counter history, rebuilt by the engine as transactions flow.",
            ["EntityAnalysisModelGatewayRuleCounterHistory"] =
                "Runtime counter history, rebuilt by the engine as transactions flow.",
            ["EntityAnalysisModelActivationRuleOverride"] =
                "Operational override raised against live traffic, with its own expiry; not definition.",
            ["EntityAnalysisModelActivationRuleSuppression"] =
                "Superseded operational suppression retained for audit; not definition.",
            ["EntityAnalysisModelOverride"] =
                "Operational override raised against live traffic, with its own expiry; not definition.",
            ["EntityAnalysisModelSuppression"] =
                "Superseded operational suppression retained for audit; not definition.",
            ["EntityAnalysisModelAsynchronousQueueBalance"] =
                "Runtime queue telemetry.",
            ["EntityAnalysisModelProcessingCounter"] =
                "Runtime processing counters.",
            ["EntityAnalysisModelResponseTimePipelineCounter"] =
                "Runtime performance counters.",
            ["EntityAnalysisModelStagePerformanceCounter"] =
                "Runtime performance counters.",
            ["EntityAnalysisModelTaskPerformanceCounter"] =
                "Runtime performance counters.",
            ["EntityAnalysisModelBacktestInstance"] =
                "A processing job over an environment's own archive; meaningless in another environment.",
            ["EntityAnalysisModelReprocessingRule"] =
                "A processing job over an environment's own archive; meaningless in another environment.",
            ["EntityAnalysisModelReprocessingRuleInstance"] =
                "A processing job over an environment's own archive; meaningless in another environment.",
            ["EntityAnalysisModelDictionaryCsvFileUpload"] =
                "Upload staging. The confirmed key value pairs are what the export carries.",
            ["EntityAnalysisModelListCsvFileUpload"] =
                "Upload staging. The confirmed list values are what the export carries.",
            ["EntityAnalysisModelEngineSnapshot"] =
                "Engine runtime state.",
            ["EntityAnalysisModelInstance"] =
                "Engine runtime state.",
            ["EntityAnalysisModelSynchronisationError"] =
                "Engine synchronisation state.",
            ["EntityAnalysisModelSynchronisationNodeStatusEntry"] =
                "Engine synchronisation state.",
            ["EntityAnalysisModelSynchronisationSchedule"] =
                "Engine synchronisation state.",
            ["EntityAnalysisModelSampleExecutionLog"] =
                "Log of sample executions.",
            ["EntityAnalysisModelSearchKeyCalculationInstance"] =
                "Cache calculation state, rebuilt by the engine.",
            ["EntityAnalysisModelSearchKeyDistinctValueCalculationInstance"] =
                "Cache calculation state, rebuilt by the engine.",
            ["VisualisationRegistryDatasourceExecutionLog"] =
                "Log of datasource executions.",
            ["VisualisationRegistryDatasourceExecutionLogParameter"] =
                "Log of datasource executions.",
            ["VisualisationRegistryDatasourceSeries"] =
                "Derived rows. Rebuilt from the datasource command's introspected columns whenever the datasource is saved."
        };

        private static List<Type> TreePocos()
        {
            return typeof(EntityAnalysisModel).Assembly.GetTypes()
                .Where(w => w.Namespace == "Jube.Data.Poco"
                            && treePrefixes.Any(p => w.Name.StartsWith(p, StringComparison.Ordinal)))
                .OrderBy(o => o.Name)
                .ToList();
        }

        private static HashSet<Type> CarriedByThePayload()
        {
            var exportSources = string.Join("\n",
                Directory.GetFiles(Path.Combine(Security.PenTest.PenTestPaths.RepositoryRoot, "Jube.Preservation"),
                        "*.cs", SearchOption.AllDirectories)
                    .Select(File.ReadAllText));

            var carried = new HashSet<Type>();
            var queue = new Queue<Type>();
            queue.Enqueue(typeof(Payload));

            while (queue.Count > 0)
            {
                var type = queue.Dequeue();

                foreach (var property in type.GetProperties()
                             .Where(w => w.GetCustomAttribute<KeyAttribute>() != null))
                {
                    var propertyType = property.PropertyType;

                    var elementType = propertyType.IsGenericType
                        ? propertyType.GetGenericArguments()[0]
                        : propertyType;

                    if (elementType.Namespace is not ("Jube.Data.Poco" or "Jube.Preservation.Models"))
                    {
                        continue;
                    }

                    if (elementType.Namespace == "Jube.Data.Poco"
                        && !exportSources.Contains(elementType.Name + "Repository")
                        && !exportSources.Contains("<" + elementType.Name + ">"))
                    {
                        continue;
                    }

                    if (carried.Add(elementType))
                    {
                        queue.Enqueue(elementType);
                    }
                }
            }

            return carried;
        }

        [Fact]
        public void EveryModelTreeTableIsCarriedOrExplicitlyExcludedWithAReason()
        {
            var carried = CarriedByThePayload();

            var missing = TreePocos()
                .Where(w => !carried.Contains(w))
                .Where(w => !w.Name.EndsWith("Version", StringComparison.Ordinal))
                .Where(w => !excluded.ContainsKey(w.Name))
                .Select(s => s.Name)
                .ToList();

            missing.Should().BeEmpty(
                "these tables belong to the tenant definition tree the export covers but are neither carried by the "
                + "export nor excluded here with a reason; version tables are excluded as a family because they are "
                + "audit copies in the way the carried approval history is not");
        }

        [Fact]
        public void TheExclusionsAreRealUncarriedTablesWithReasons()
        {
            var carried = CarriedByThePayload();
            var names = TreePocos().Select(s => s.Name).ToHashSet();

            foreach (var (name, reason) in excluded)
            {
                names.Should().Contain(name, "an exclusion must name a real table");
                carried.Select(s => s.Name).Should().NotContain(name, "a carried table needs no exclusion");
                reason.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public void EveryApprovableKindIsCarriedSoItsHistoryCanReplay()
        {
            var carried = CarriedByThePayload().Select(s => s.Name).ToHashSet();

            foreach (var kind in Enum.GetValues<EntityApprovalKind>())
            {
                var table = kind == EntityApprovalKind.EntityAnalysisModelRequestXPath
                    ? "EntityAnalysisModelRequestXpath"
                    : kind.ToString();

                carried.Should().Contain(table,
                    $"the approval history of kind {kind} is exported, so the entity it approves must travel too");
            }

            carried.Should().Contain(nameof(EntityApproval),
                "the approval history itself is part of the payload");
        }
    }
}
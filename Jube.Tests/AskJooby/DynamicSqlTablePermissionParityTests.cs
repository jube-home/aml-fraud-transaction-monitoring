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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.AskJooby.Security;
using Jube.Service.Agent.Models;
using Jube.Service.DynamicSql;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class DynamicSqlTablePermissionParityTests
    {
        private static IReadOnlyDictionary<string, int[]> ServicePermissions()
        {
            var permissions = new Dictionary<string, int[]>();

            foreach (var type in typeof(ServiceOperationAttribute).Assembly.GetTypes()
                         .Where(t => t is { IsClass: true } && t.Name.EndsWith("Service")))
            {
                var field = type.GetField("permissions", BindingFlags.NonPublic | BindingFlags.Static);

                if (field?.GetValue(null) is int[] declared && declared.Length > 0)
                {
                    permissions[type.Name[..^"Service".Length]] = declared;
                }
            }

            return permissions;
        }

        [Fact]
        public void EveryRegisteredTableWithAnOwningServiceDemandsThatServicesOwnPermissions()
        {
            var services = ServicePermissions();
            var drifted = new List<string>();

            foreach (var table in DynamicSqlTableAccess.RegisteredTables)
            {
                var owner = table.EndsWith("Version") ? table[..^"Version".Length] : table;

                if (!services.TryGetValue(owner, out var expected))
                {
                    continue;
                }

                var declared = DynamicSqlTableAccess.RequiredPermissionsFor(table);

                if (!declared.OrderBy(p => p).SequenceEqual(expected.OrderBy(p => p)))
                {
                    drifted.Add($"{table} allows [{string.Join(",", declared)}] "
                                + $"but {owner}Service enforces [{string.Join(",", expected)}]");
                }
            }

            drifted.Should().BeEmpty(
                "a table queryable under a weaker permission than its own service enforces is a way "
                + "round that service: " + string.Join("; ", drifted));
        }

        [Fact]
        public void TheCredentialTablesAreNotQueryableOnTheBareAssistantPermission()
        {
            var allowed = DynamicSqlTableAccess.BuildAllowedTables(
                "alice", p => p.Contains(AskJoobyPermissions.Access));

            allowed.Should().NotContainKey("UserRegistry")
                .And.NotContainKey("UserRegistryApiKey")
                .And.NotContainKey("RoleRegistry")
                .And.NotContainKey("RoleRegistryPermission");
        }

        [Fact]
        public void TheExhaustiveFamilyDemandsTheExhaustivePagePermission()
        {
            foreach (var table in DynamicSqlTableAccess.RegisteredTables
                         .Where(t => t.StartsWith("ExhaustiveSearchInstance")))
            {
                DynamicSqlTableAccess.RequiredPermissionsFor(table).Should().Equal([16],
                    $"{table} belongs to the Exhaustive surface, whose services all enforce [16]");
            }
        }

        [Fact]
        public void TheTablesWithDifferentlyNamedOwningServicesDemandThoseServicesOwnPermissions()
        {
            var pinned = new Dictionary<string, int[]>
            {
                ["ArchiveEntityAnalysisModelAbstractionEntry"] = [1],
                ["ArchiveKey"] = [1],
                ["ArchiveKeyVersion"] = [1],
                ["ArchiveTag"] = [1],
                ["ArchiveTagVersion"] = [1],
                ["MockArchive"] = [1],
                ["UserInTenantSwitchLog"] = [1],
                ["EntityAnalysisModelActivationRuleCounterHistory"] = [17],
                ["EntityAnalysisModelGatewayRuleCounterHistory"] = [10],
                ["EntityAnalysisModelDictionaryCsvFileUpload"] = [4],
                ["EntityAnalysisModelListCsvFileUpload"] = [3],
                ["EntityAnalysisModelSampleExecutionLog"] = [40],
                ["EntityAnalysisModelSynchronisationNodeStatusEntry"] = [5],
                ["EntityAnalysisModelOverride"] = [2],
                ["EntityAnalysisModelOverrideVersion"] = [2],
                ["EntityAnalysisModelActivationRuleOverride"] = [2],
                ["EntityAnalysisModelActivationRuleOverrideVersion"] = [2],
                ["Export"] = [38],
                ["PreservationDiff"] = [38],
                ["Import"] = [38],
                ["PreservationSnapshot"] = [38],
                ["EntityApproval"] = [45, 44],
                ["EntityAnalysisModelBacktestInstance"] = [61],
                ["VisualisationRegistry"] = [31, 28, 1],
                ["VisualisationRegistryVersion"] = [31, 28, 1],
                ["VisualisationRegistryDatasource"] = [33, 28, 1],
                ["VisualisationRegistryDatasourceVersion"] = [33, 28, 1],
                ["VisualisationRegistryDatasourceExecutionLog"] = [33, 28, 1],
                ["VisualisationRegistryDatasourceExecutionLogParameter"] = [33, 28, 1]
            };

            var drifted = new List<string>();

            foreach (var (table, expected) in pinned)
            {
                var declared = DynamicSqlTableAccess.RequiredPermissionsFor(table);

                if (!declared.OrderBy(p => p).SequenceEqual(expected.OrderBy(p => p)))
                {
                    drifted.Add($"{table} allows [{string.Join(",", declared)}] "
                                + $"but its owning service enforces [{string.Join(",", expected)}]");
                }
            }

            drifted.Should().BeEmpty(
                "the automatic parity test cannot see these tables because their owning service is named "
                + "differently, so their permissions are pinned here: " + string.Join("; ", drifted));
        }
    }
}
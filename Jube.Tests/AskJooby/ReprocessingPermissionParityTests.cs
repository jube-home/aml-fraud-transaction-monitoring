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
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.Migrations.Branches.ReprocessingPermissions;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.DynamicSql;
using Jube.Service.EntityAnalysisModelReprocessingRule;
using Jube.Service.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Service.Query.TreeChildren;
using Jube.Service.Security;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class ReprocessingPermissionParityTests
    {
        private static readonly string[] ruleEditTools =
        [
            "Create", "Update", "Delete", "Validate", "ParseRule", "Execute", "BuildRuleFromBuilderJson"
        ];

        private static readonly string[] ruleReadTools =
        [
            "List", "FilterFields", "Filter", "Count", "Get", "GetByEntityAnalysisModelId"
        ];

        private static readonly string[] instanceTriggerTools =
        [
            "Create", "Update", "Validate", "InsertByExistingUpdateUncompleted"
        ];

        private static readonly string[] instanceReadTools =
        [
            "List", "FilterFields", "Filter", "Count", "Get", "GetByEntityAnalysisModelReprocessingId"
        ];

        private static IReadOnlyList<int> PermissionsOf(string tool)
        {
            return ServiceToolCatalogue.All.Single(t => t.Name == tool).RequiredPermissions;
        }

        [Fact]
        public void TheOriginalPermissionKeepsItsNumberAndTheTwoNewOnesAreTheNextFreeNumbers()
        {
            ReprocessingPermissions.EditRuleId.Should().Be(26);
            ReprocessingPermissions.TriggerRunId.Should().Be(73);
            ReprocessingPermissions.StopRunId.Should().Be(74);

            SplitReprocessingPermission.EditRulePermissionId.Should().Be(ReprocessingPermissions.EditRuleId);
            SplitReprocessingPermission.TriggerPermissionId.Should().Be(ReprocessingPermissions.TriggerRunId);
            SplitReprocessingPermission.StopPermissionId.Should().Be(ReprocessingPermissions.StopRunId);
        }

        [Fact]
        public void EveryRuleEditToolDemandsTheServicesEditPermissionAndNothingElse()
        {
            foreach (var tool in ruleEditTools)
            {
                PermissionsOf("EntityAnalysisModelReprocessingRule" + tool).Should()
                    .Equal(EntityAnalysisModelReprocessingRuleService.EditPermissions, tool);
            }

            EntityAnalysisModelReprocessingRuleService.EditPermissions.Should().Equal(26);
        }

        [Fact]
        public void EveryRuleReadToolDemandsAnyOfTheThreePermissions()
        {
            foreach (var tool in ruleReadTools)
            {
                PermissionsOf("EntityAnalysisModelReprocessingRule" + tool).Should()
                    .BeEquivalentTo(EntityAnalysisModelReprocessingRuleService.RequiredPermissions, tool);
            }

            EntityAnalysisModelReprocessingRuleService.RequiredPermissions.Should().BeEquivalentTo(new[] { 26, 73, 74 });
        }

        [Fact]
        public void EveryInstanceTriggerToolDemandsOnlyTheTriggerPermission()
        {
            foreach (var tool in instanceTriggerTools)
            {
                PermissionsOf("EntityAnalysisModelReprocessingRuleInstance" + tool).Should().Equal(new[] { 73 }, tool);
            }

            EntityAnalysisModelReprocessingRuleInstanceService.TriggerPermissions.Should().Equal(73);
        }

        [Fact]
        public void TheInstanceDeleteToolDemandsOnlyTheStopPermission()
        {
            PermissionsOf("EntityAnalysisModelReprocessingRuleInstanceDelete").Should().Equal(74);
            EntityAnalysisModelReprocessingRuleInstanceService.StopPermissions.Should().Equal(74);
        }

        [Fact]
        public void EveryInstanceReadToolDemandsAnyOfTheThreePermissions()
        {
            foreach (var tool in instanceReadTools)
            {
                PermissionsOf("EntityAnalysisModelReprocessingRuleInstance" + tool).Should()
                    .BeEquivalentTo(new[] { 26, 73, 74 }, tool);
            }
        }

        [Fact]
        public void TheTreeNodeAndBothSqlTablesAdmitEveryHolderOfAnyOfTheThreePermissions()
        {
            TreeChildrenNodes.Reprocessing.Permissions.Should().BeEquivalentTo(new[] { 26, 73, 74 });

            DynamicSqlTableAccess.RequiredPermissionsFor("EntityAnalysisModelReprocessingRule").Should()
                .BeEquivalentTo(new[] { 26, 73, 74 });
            DynamicSqlTableAccess.RequiredPermissionsFor("EntityAnalysisModelReprocessingRuleInstance").Should()
                .BeEquivalentTo(new[] { 26, 73, 74 });
        }

        [Fact]
        public void EveryHubOnThePathToReprocessingIsReachableWithOnlyTheTriggerOrOnlyTheStopPermission()
        {
            foreach (var slug in new[] { "model-admin", "model-support", "model-support.navigation", "model-support.reprocessing" })
            {
                AgentDefinitions.All[slug].RequiredPermissions.Should().Contain(new[] { 73, 74 }, slug);
            }
        }
    }
}

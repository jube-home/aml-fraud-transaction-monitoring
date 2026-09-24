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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelReprocessingRuleInstance(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceList", OperationKind.Read, true, false,
                    "Lists Reprocessing Rule instances for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Reprocessing Rule Instances may use."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceFilter", OperationKind.Read, true, false,
                    "Returns the Reprocessing Rule Instances matching query builder JSON, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceCount", OperationKind.Read, true, false,
                    "Counts the Reprocessing Rule Instances matching query builder JSON, optionally grouped by a field."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceGet", OperationKind.Read, true, false,
                    "Returns one Reprocessing Rule instance by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceGetByEntityAnalysisModelReprocessingId",
                    OperationKind.Read, true, false,
                    "Lists Reprocessing Rule instances belonging to a given Reprocessing Rule, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceCreate", OperationKind.Write, false, false,
                    "Registers a Reprocessing Rule instance under a Reprocessing Rule in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceValidate", OperationKind.Read, true, false,
                    "Validates a Reprocessing Rule Instance without saving it and returns every failure."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceInsertByExistingUpdateUncompleted",
                    OperationKind.Write, false, false,
                    "Triggers a new reprocessing run for a Reprocessing Rule; fails if one is already uncompleted."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceUpdate", OperationKind.Write, false, false,
                    "Supersedes a Reprocessing Rule instance in the caller's tenant by id, assigning it a new id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleInstanceDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Reprocessing Rule instance in the caller's tenant by id.")
            ]);
        }
    }
}
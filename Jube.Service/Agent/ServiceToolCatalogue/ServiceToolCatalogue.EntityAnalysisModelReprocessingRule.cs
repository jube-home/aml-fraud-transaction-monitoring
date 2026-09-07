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
        static partial void AddEntityAnalysisModelReprocessingRule(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleList", OperationKind.Read, true, false,
                    "Lists Reprocessing Rules for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleGet", OperationKind.Read, true, false,
                    "Returns one Reprocessing Rule by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false,
                    "Lists Reprocessing Rules belonging to a given Model, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleCreate", OperationKind.Write, false, false,
                    "Registers a Reprocessing Rule under a Model in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleUpdate", OperationKind.Write, false, false,
                    "Supersedes a Reprocessing Rule in the caller's tenant by id, assigning it a new id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Reprocessing Rule in the caller's tenant by id.")
            ]);
        }
    }
}

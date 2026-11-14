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

using Jube.Service.Agent.Models;
using Jube.Service.Repository.EntityAnalysisModelGraphRecall;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelGraphRecall(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallList", OperationKind.Read, true, false,
                    "Lists graph recall entities visible to the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelGraphRecallService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over graph recall entities may use.",
                    EntityAnalysisModelGraphRecallService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallFilter", OperationKind.Read, true, false,
                    "Returns the graph recall entities matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelGraphRecallService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallCount", OperationKind.Read, true, false,
                    "Counts the graph recall entities matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelGraphRecallService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallListByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Lists the graph recall entities of one model.",
                    EntityAnalysisModelGraphRecallService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallGet", OperationKind.Read, true, false,
                    "Returns a single graph recall entity by Id.",
                    EntityAnalysisModelGraphRecallService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallFields", OperationKind.Read, true, false,
                    "Lists the request fields of one model a graph recall entity may name, with their Archive Key flag.",
                    EntityAnalysisModelGraphRecallService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallCreate", OperationKind.Write, false, false,
                    "Creates a graph recall entity (a model field read from the graph as Graph.{Name} in rules); calling twice creates two.",
                    EntityAnalysisModelGraphRecallService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallValidate", OperationKind.Read, true, false,
                    "Validates a graph recall entity without saving it and returns every failure.",
                    EntityAnalysisModelGraphRecallService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallUpdate", OperationKind.Write, true, false,
                    "Updates a graph recall entity; overwrites, writes a version-audit row.",
                    EntityAnalysisModelGraphRecallService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a graph recall entity; reversible only by direct database action.",
                    EntityAnalysisModelGraphRecallService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected graph recall entity for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelGraphRecallService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGraphRecallRetract", OperationKind.Write, false, false,
                    "Retracts a rejected graph recall entity, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelGraphRecallService.WritePermissions)
            ]);
        }
    }
}

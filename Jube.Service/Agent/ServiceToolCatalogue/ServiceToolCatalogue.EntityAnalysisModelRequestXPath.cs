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
using Jube.Service.EntityAnalysisModelRequestXPath;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelRequestXPath(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathList", OperationKind.Read, true, false,
                    "Lists Request XPaths for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelRequestXPathService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over RequestXPaths may use.",
                    EntityAnalysisModelRequestXPathService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathFilter", OperationKind.Read, true, false,
                    "Returns the RequestXPaths matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelRequestXPathService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathCount", OperationKind.Read, true, false,
                    "Counts the RequestXPaths matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelRequestXPathService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathGet", OperationKind.Read, true, false,
                    "Returns one Request XPath by id, scoped to the caller's tenant.",
                    EntityAnalysisModelRequestXPathService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false, "Lists Request XPaths belonging to a given Model, scoped to the caller's tenant.",
                    EntityAnalysisModelRequestXPathService.ReadByEntityAnalysisModelPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathGetByCasesWorkflowId", OperationKind.Read, true,
                    false,
                    "Lists Request XPaths reachable from a given Case Workflow's Model, scoped to the caller's tenant.",
                    EntityAnalysisModelRequestXPathService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathGetByOverrideKey", OperationKind.Read, true,
                    false, "Lists Request XPaths flagged for Override, scoped to the caller's tenant.",
                    EntityAnalysisModelRequestXPathService.OverridePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathGetByEntityAnalysisModelIdByDataType", OperationKind.Read,
                    true, false,
                    "Lists String, Integer or Float Request XPaths belonging to a given Model, scoped to the caller's " +
                    "tenant.",
                    EntityAnalysisModelRequestXPathService.ReadByDataTypePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathCreate", OperationKind.Write, false, false,
                    "Creates a Request XPath under a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelRequestXPathService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathValidate", OperationKind.Read, true, false,
                    "Validates a RequestXPath without saving it and returns every failure.",
                    EntityAnalysisModelRequestXPathService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathUpdate", OperationKind.Write, true, false,
                    "Updates a Request XPath in the caller's tenant by id.",
                    EntityAnalysisModelRequestXPathService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Request XPath in the caller's tenant by id.",
                    EntityAnalysisModelRequestXPathService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected Request XPath for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelRequestXPathService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRequestXPathRetract", OperationKind.Write, false, false,
                    "Retracts a rejected Request XPath, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelRequestXPathService.WritePermissions)
            ]);
        }
    }
}
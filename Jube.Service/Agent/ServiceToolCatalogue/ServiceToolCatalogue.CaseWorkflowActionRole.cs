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
using Jube.Service.Repository.CaseWorkflowActionRole;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowActionRole(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowActionRoleListByCaseWorkflowActionGuid", OperationKind.Read, true,
                    false,
                    "Lists the Role grants for a Case Workflow Action.",
                    CaseWorkflowActionRoleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionRoleCreate", OperationKind.Write, false, false,
                    "Grants a Role access to a Case Workflow Action; calling twice creates two grants.",
                    CaseWorkflowActionRoleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionRoleDelete", OperationKind.Delete, false, true,
                    "Removes a Role's grant on a Case Workflow Action; reversible only by direct database action. " +
                    "A repeat call on an already-removed grant throws rather than succeeding as a no-op, so retries " +
                    "are not safe to assume succeeded.",
                    CaseWorkflowActionRoleService.RequiredPermissions)
            ]);
        }
    }
}
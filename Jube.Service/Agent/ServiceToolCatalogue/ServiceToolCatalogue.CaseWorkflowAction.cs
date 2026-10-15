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
using Jube.Service.Repository.CaseWorkflowAction;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowAction(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowActionList", OperationKind.Read, true, false,
                    "Lists Case Workflow Actions visible to the caller's tenant, keyset-paged and capped.",
                    CaseWorkflowActionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Case Workflow Actions may use.",
                    CaseWorkflowActionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionFilter", OperationKind.Read, true, false,
                    "Returns the Case Workflow Actions matching query builder JSON, keyset-paged and capped.",
                    CaseWorkflowActionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionCount", OperationKind.Read, true, false,
                    "Counts the Case Workflow Actions matching query builder JSON, optionally grouped by a field.",
                    CaseWorkflowActionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionListByWorkflowIdActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Actions for a parent Case Workflow that the caller holds a role " +
                    "grant on.",
                    CaseWorkflowActionService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionListByWorkflowGuidActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Actions for a parent Case Workflow (by Guid) that the caller holds " +
                    "a role grant on.",
                    CaseWorkflowActionService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionGet", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Action by Id.",
                    CaseWorkflowActionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Action by Id.",
                    CaseWorkflowActionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow Action; calling twice creates two.",
                    CaseWorkflowActionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionValidate", OperationKind.Read, true, false,
                    "Validates a Case Workflow Action without saving it and returns every failure.",
                    CaseWorkflowActionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow Action; overwrites, writes a version-audit row.",
                    CaseWorkflowActionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowActionDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow Action; reversible only by direct database action.",
                    CaseWorkflowActionService.RequiredPermissions)
            ]);
        }
    }
}
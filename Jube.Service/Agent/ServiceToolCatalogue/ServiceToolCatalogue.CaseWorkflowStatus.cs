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
using Jube.Service.Repository.CaseWorkflowStatus;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowStatus(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusList", OperationKind.Read, true, false,
                    "Lists Case Workflow Statuses visible to the caller's tenant, keyset-paged and capped.",
                    CaseWorkflowStatusService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Case Workflow Statuses may use.",
                    CaseWorkflowStatusService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusFilter", OperationKind.Read, true, false,
                    "Returns the Case Workflow Statuses matching query builder JSON, keyset-paged and capped.",
                    CaseWorkflowStatusService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusCount", OperationKind.Read, true, false,
                    "Counts the Case Workflow Statuses matching query builder JSON, optionally grouped by a field.",
                    CaseWorkflowStatusService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusListByWorkflowGuid", OperationKind.Read, true, false,
                    "Lists Case Workflow Statuses for a parent Case Workflow (by Guid), including inactive ones.",
                    CaseWorkflowStatusService.ListByWorkflowPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusListByWorkflowIdActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Statuses for a parent Case Workflow that the caller holds a role " +
                    "grant on.",
                    CaseWorkflowStatusService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusListByWorkflowGuidActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Statuses for a parent Case Workflow (by Guid) that the caller " +
                    "holds a role grant on.",
                    CaseWorkflowStatusService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusGet", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Status by Id.",
                    CaseWorkflowStatusService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Status by Id.",
                    CaseWorkflowStatusService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow Status; calling twice creates two.",
                    CaseWorkflowStatusService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusValidate", OperationKind.Read, true, false,
                    "Validates a Case Workflow Status without saving it and returns every failure.",
                    CaseWorkflowStatusService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow Status; overwrites, writes a version-audit row.",
                    CaseWorkflowStatusService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowStatusDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow Status; reversible only by direct database action.",
                    CaseWorkflowStatusService.RequiredPermissions)
            ]);
        }
    }
}
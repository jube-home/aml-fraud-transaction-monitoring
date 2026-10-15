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
using Jube.Service.Repository.CaseWorkflowFilter;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowFilter(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterList", OperationKind.Read, true, false,
                    "Lists Case Workflow Filters visible to the caller's tenant, keyset-paged and capped.",
                    CaseWorkflowFilterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Case Workflow Filters may use.",
                    CaseWorkflowFilterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterFilter", OperationKind.Read, true, false,
                    "Returns the Case Workflow Filters matching query builder JSON, keyset-paged and capped.",
                    CaseWorkflowFilterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterCount", OperationKind.Read, true, false,
                    "Counts the Case Workflow Filters matching query builder JSON, optionally grouped by a field.",
                    CaseWorkflowFilterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterListByWorkflowIdActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Filters for a parent Case Workflow that the caller holds a role " +
                    "grant on.",
                    CaseWorkflowFilterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterListByWorkflowGuidActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Filters for a parent Case Workflow (by Guid) that the caller holds " +
                    "a role grant on.",
                    CaseWorkflowFilterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterGet", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Filter by Id.",
                    CaseWorkflowFilterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Filter by Guid.",
                    CaseWorkflowFilterService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow Filter; calling twice creates two.",
                    CaseWorkflowFilterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterValidate", OperationKind.Read, true, false,
                    "Validates a Case Workflow Filter without saving it and returns every failure.",
                    CaseWorkflowFilterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow Filter; overwrites, writes a version-audit row.",
                    CaseWorkflowFilterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow Filter; reversible only by direct database action.",
                    CaseWorkflowFilterService.RequiredPermissions)
            ]);
        }
    }
}
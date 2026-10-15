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
using Jube.Service.Repository.CaseWorkflowDisplay;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowDisplay(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayList", OperationKind.Read, true, false,
                    "Lists Case Workflow Displays visible to the caller's tenant, keyset-paged and capped.",
                    CaseWorkflowDisplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Case Workflow Displays may use.",
                    CaseWorkflowDisplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayFilter", OperationKind.Read, true, false,
                    "Returns the Case Workflow Displays matching query builder JSON, keyset-paged and capped.",
                    CaseWorkflowDisplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayCount", OperationKind.Read, true, false,
                    "Counts the Case Workflow Displays matching query builder JSON, optionally grouped by a field.",
                    CaseWorkflowDisplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayListByWorkflowIdActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Displays for a parent Case Workflow that the caller holds a role " +
                    "grant on.",
                    CaseWorkflowDisplayService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayListByWorkflowGuidActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Displays for a parent Case Workflow (by Guid) that the caller holds " +
                    "a role grant on.",
                    CaseWorkflowDisplayService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayGet", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Display by Id.",
                    CaseWorkflowDisplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Display by Id.",
                    CaseWorkflowDisplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow Display; calling twice creates two.",
                    CaseWorkflowDisplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayValidate", OperationKind.Read, true, false,
                    "Validates a Case Workflow Display without saving it and returns every failure.",
                    CaseWorkflowDisplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow Display; overwrites, writes a version-audit row.",
                    CaseWorkflowDisplayService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow Display; reversible only by direct database action.",
                    CaseWorkflowDisplayService.RequiredPermissions)
            ]);
        }
    }
}
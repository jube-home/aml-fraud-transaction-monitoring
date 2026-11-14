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
using Jube.Service.Repository.CaseWorkflowChecklistItem;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowChecklistItem(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistItemList", OperationKind.Read, true, false,
                    "Lists every Case Workflow checklist item visible to the caller's tenant, ordered by priority.",
                    CaseWorkflowChecklistItemService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistItemListByCaseWorkflowId", OperationKind.Read, true, false,
                    "Lists the checklist items of the statuses of a Case Workflow, ordered by priority.",
                    CaseWorkflowChecklistItemService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistItemListByCaseWorkflowChecklistGuid", OperationKind.Read, true, false,
                    "Lists the items of one Case Workflow checklist, ordered by priority.",
                    CaseWorkflowChecklistItemService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistItemGet", OperationKind.Read, true, false,
                    "Returns one Case Workflow checklist item by Id.",
                    CaseWorkflowChecklistItemService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistItemValidate", OperationKind.Read, true, false,
                    "Runs every check a create or update of a Case Workflow checklist item would run, storing nothing.",
                    CaseWorkflowChecklistItemService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistItemCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow checklist item: a name and priority with an optional show SQL, " +
                    "validate SQL and SLA in minutes. Any SQL also needs the SQL authoring permission.",
                    CaseWorkflowChecklistItemService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistItemUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow checklist item. Changing its show or validate SQL also needs the SQL " +
                    "authoring permission.",
                    CaseWorkflowChecklistItemService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistItemDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow checklist item; reversible only by direct database action.",
                    CaseWorkflowChecklistItemService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistItemTest", OperationKind.Read, true, false,
                    "Tries the show or validate SQL of a saved Case Workflow checklist item against a case without " +
                    "ticking anything, and reports whether it passed, why not and how long it took.",
                    CaseWorkflowChecklistItemService.RequiredPermissions)
            ]);
        }
    }
}

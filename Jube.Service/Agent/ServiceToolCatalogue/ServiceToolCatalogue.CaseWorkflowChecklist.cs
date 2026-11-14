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
using Jube.Service.Repository.CaseWorkflowChecklist;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowChecklist(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistList", OperationKind.Read, true, false,
                    "Lists every Case Workflow checklist visible to the caller's tenant, ordered by Priority ascending then name.",
                    CaseWorkflowChecklistService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistListByCaseWorkflowStatusId", OperationKind.Read, true, false,
                    "Lists the checklists carried by a Case Workflow Status, ordered by Priority ascending then name.",
                    CaseWorkflowChecklistService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistGet", OperationKind.Read, true, false,
                    "Returns one Case Workflow checklist by Id.",
                    CaseWorkflowChecklistService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow checklist carried by one Case Workflow Status. Every item shown on a case " +
                    "in the status must be ticked before the case can leave the status or be closed.",
                    CaseWorkflowChecklistService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistValidate", OperationKind.Read, true, false,
                    "Runs every check a create or update of a Case Workflow checklist would run, storing nothing.",
                    CaseWorkflowChecklistService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow checklist; the status that carries it cannot be changed.",
                    CaseWorkflowChecklistService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowChecklistDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow checklist; reversible only by direct database action.",
                    CaseWorkflowChecklistService.RequiredPermissions)
            ]);
        }
    }
}

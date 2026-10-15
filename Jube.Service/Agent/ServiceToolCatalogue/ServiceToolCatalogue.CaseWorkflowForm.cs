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
using Jube.Service.Repository.CaseWorkflowForm;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowForm(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowFormList", OperationKind.Read, true, false,
                    "Lists Case Workflow Forms visible to the caller's tenant, keyset-paged and capped.",
                    CaseWorkflowFormService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Case Workflow Forms may use.",
                    CaseWorkflowFormService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormFilter", OperationKind.Read, true, false,
                    "Returns the Case Workflow Forms matching query builder JSON, keyset-paged and capped.",
                    CaseWorkflowFormService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormCount", OperationKind.Read, true, false,
                    "Counts the Case Workflow Forms matching query builder JSON, optionally grouped by a field.",
                    CaseWorkflowFormService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormListByWorkflowIdActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Forms for a parent Case Workflow that the caller holds a role " +
                    "grant on.",
                    CaseWorkflowFormService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormListByWorkflowGuidActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Forms for a parent Case Workflow (by Guid) that the caller holds " +
                    "a role grant on.",
                    CaseWorkflowFormService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormGet", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Form by Id.",
                    CaseWorkflowFormService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Form by Id.",
                    CaseWorkflowFormService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow Form; calling twice creates two.",
                    CaseWorkflowFormService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormValidate", OperationKind.Read, true, false,
                    "Validates a Case Workflow Form without saving it and returns every failure.",
                    CaseWorkflowFormService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow Form; overwrites, writes a version-audit row.",
                    CaseWorkflowFormService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow Form; reversible only by direct database action.",
                    CaseWorkflowFormService.RequiredPermissions)
            ]);
        }
    }
}
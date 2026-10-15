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
using Jube.Service.Repository.CaseWorkflow;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflow(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowList", OperationKind.Read, true, false,
                    "Lists Case Workflows visible to the caller's tenant, keyset-paged and capped.",
                    CaseWorkflowService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Case Workflows may use.",
                    CaseWorkflowService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFilter", OperationKind.Read, true, false,
                    "Returns the Case Workflows matching query builder JSON, keyset-paged and capped.",
                    CaseWorkflowService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowCount", OperationKind.Read, true, false,
                    "Counts the Case Workflows matching query builder JSON, optionally grouped by a field.",
                    CaseWorkflowService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowListByModelActiveOnly", OperationKind.Read, true, false,
                    "Lists active Case Workflows for a parent model that the caller holds a role grant on.",
                    CaseWorkflowService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowGet", OperationKind.Read, true, false,
                    "Returns a single Case Workflow by Id.",
                    CaseWorkflowService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Case Workflow by Id.",
                    CaseWorkflowService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow; calling twice creates two.",
                    CaseWorkflowService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowValidate", OperationKind.Read, true, false,
                    "Validates a Case Workflow without saving it and returns every failure.",
                    CaseWorkflowService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow; overwrites, writes a version-audit row.",
                    CaseWorkflowService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow; reversible only by direct database action.",
                    CaseWorkflowService.RequiredPermissions)
            ]);
        }
    }
}
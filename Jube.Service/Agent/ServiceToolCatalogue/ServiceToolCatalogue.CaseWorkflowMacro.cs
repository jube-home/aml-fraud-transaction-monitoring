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
using Jube.Service.Repository.CaseWorkflowMacro;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowMacro(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroList", OperationKind.Read, true, false,
                    "Lists Case Workflow Macros visible to the caller's tenant, keyset-paged and capped.",
                    CaseWorkflowMacroService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Case Workflow Macros may use.",
                    CaseWorkflowMacroService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroFilter", OperationKind.Read, true, false,
                    "Returns the Case Workflow Macros matching query builder JSON, keyset-paged and capped.",
                    CaseWorkflowMacroService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroCount", OperationKind.Read, true, false,
                    "Counts the Case Workflow Macros matching query builder JSON, optionally grouped by a field.",
                    CaseWorkflowMacroService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroListByWorkflowIdActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Macros for a parent Case Workflow that the caller holds a role " +
                    "grant on.",
                    CaseWorkflowMacroService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroListByWorkflowGuidActiveOnly", OperationKind.Read, true,
                    false,
                    "Lists active Case Workflow Macros for a parent Case Workflow (by Guid) that the caller holds " +
                    "a role grant on.",
                    CaseWorkflowMacroService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroGet", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Macro by Id.",
                    CaseWorkflowMacroService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Case Workflow Macro by Id.",
                    CaseWorkflowMacroService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow Macro; calling twice creates two.",
                    CaseWorkflowMacroService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroValidate", OperationKind.Read, true, false,
                    "Validates a Case Workflow Macro without saving it and returns every failure.",
                    CaseWorkflowMacroService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow Macro; overwrites, writes a version-audit row.",
                    CaseWorkflowMacroService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowMacroDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow Macro; reversible only by direct database action.",
                    CaseWorkflowMacroService.RequiredPermissions)
            ]);
        }
    }
}
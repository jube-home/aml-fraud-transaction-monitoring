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
using Jube.Service.Repository.CaseWorkflowFormValidator;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowFormValidator(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowFormValidatorList", OperationKind.Read, true, false,
                    "Lists every server-side Case Workflow Form validator visible to the caller's tenant.",
                    CaseWorkflowFormValidatorService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormValidatorListByCaseWorkflowId", OperationKind.Read, true, false,
                    "Lists the server-side validators of the Forms of a Case Workflow, each paired to a template field by name.",
                    CaseWorkflowFormValidatorService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormValidatorGet", OperationKind.Read, true, false,
                    "Returns one Case Workflow Form validator by Id.",
                    CaseWorkflowFormValidatorService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormValidatorCreate", OperationKind.Write, false, false,
                    "Creates a server-side validator for one field of a Case Workflow Form: required, required " +
                    "when, range, length, pattern, compare, in a model list, in a model dictionary or SQL. " +
                    "A SQL validator also needs the SQL authoring permission.",
                    CaseWorkflowFormValidatorService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormValidatorValidate", OperationKind.Read, true, false,
                    "Runs every check a create or update of a Case Workflow Form validator would run, storing nothing.",
                    CaseWorkflowFormValidatorService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormValidatorUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow Form validator. Changing a SQL validator also needs the SQL authoring " +
                    "permission.",
                    CaseWorkflowFormValidatorService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormValidatorDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow Form validator; reversible only by direct database action.",
                    CaseWorkflowFormValidatorService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormValidatorTest", OperationKind.Read, true, false,
                    "Tries a saved Case Workflow Form validator against given values without submitting the form, " +
                    "and reports whether it passed, why not and how long it took.",
                    CaseWorkflowFormValidatorService.RequiredPermissions)
            ]);
        }
    }
}

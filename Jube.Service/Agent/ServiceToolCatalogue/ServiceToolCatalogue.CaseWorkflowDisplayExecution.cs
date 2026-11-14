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
using Jube.Service.CaseDispatch;
using Jube.Service.Query.CaseWorkflowDisplayExecution;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowDisplayExecution(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayExecutionExecute", OperationKind.Read, true, false,
                    "Renders a case workflow display template as an escaped HTML fragment from the case's stored payload.",
                    CaseWorkflowDisplayExecutionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayExecutionRenderForm", OperationKind.Read, true, false,
                    "Renders a case workflow form template with the values entered so far, naming the fields shown.",
                    CaseWorkflowDisplayExecutionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayExecutionRenderFormEntries", OperationKind.Read, true, false,
                    "Draws the submitted form entries for a case's key read only, with their values filled in.",
                    CaseWorkflowDisplayExecutionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayExecutionListDispatches", OperationKind.Read, true, false,
                    "Lists the audit of the emails, SMS and HTTP calls fired for a case's key: what was rendered, " +
                    "where it went, whether it was sent, failed or refused, and the SQL rows it was drawn from.",
                    CaseWorkflowDisplayExecutionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowDisplayExecutionTestSql", OperationKind.Read, true, false,
                    "Tries the SQL of a Form, Display, Macro, Action or Status against a case without saving it, " +
                    "returning the columns and first rows or why it was refused. Needs the case workflow SQL " +
                    "authoring permission.",
                    CaseSqlGuard.RequiredPermissions)
            ]);
        }
    }
}
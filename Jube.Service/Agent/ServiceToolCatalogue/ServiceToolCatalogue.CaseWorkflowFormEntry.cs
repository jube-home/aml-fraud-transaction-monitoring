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
using Jube.Service.Repository.CaseWorkflowFormEntry;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowFormEntry(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowFormEntryInsert", OperationKind.Write, false, false,
                    "Submits a CaseWorkflowForm entry for a case; dispatches that form's notification/webhook if " +
                    "enabled.",
                    CaseWorkflowFormEntryService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormEntryValidate", OperationKind.Read, true, false,
                    "Validates a Case Workflow Form Entry without saving it and returns every failure.",
                    CaseWorkflowFormEntryService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowFormEntryListByCaseKeyValue", OperationKind.Read, true, false,
                    "Lists case workflow form entries for a Case Key/Value pair, newest first, scoped to the caller's " +
                    "tenant and role grants.",
                    CaseWorkflowFormEntryService.RequiredPermissions)
            ]);
        }
    }
}
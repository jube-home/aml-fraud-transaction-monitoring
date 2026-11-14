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
using Jube.Service.Query.CaseChecklist;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseChecklist(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseChecklistGet", OperationKind.Read, true, false,
                    "Gets the checklists of the status a case is in, after bringing them up to date: which items " +
                    "are shown or done, each item's SLA position and minutes left, and whether everything " +
                    "required is done. May record items shown or hidden on the case timeline.",
                    CaseChecklistService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseChecklistCheck", OperationKind.Write, false, false,
                    "Ticks one shown item of the checklist of the status a case is in. Only the user who holds the " +
                    "case can tick, and the item's validation condition, if it has one, must pass first; a refusal " +
                    "is recorded and the item stays unticked. Records the tick on the case timeline.",
                    CaseChecklistService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseChecklistUncheck", OperationKind.Write, false, false,
                    "Unticks one ticked item of the checklist of the status a case is in, putting it back to shown " +
                    "without restarting its SLA clock. Only the user who holds the case can untick. Records the " +
                    "untick on the case timeline.",
                    CaseChecklistService.RequiredPermissions)
            ]);
        }
    }
}

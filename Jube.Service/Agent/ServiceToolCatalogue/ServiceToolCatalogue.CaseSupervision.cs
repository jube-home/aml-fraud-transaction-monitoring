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
using Jube.Service.Query.CaseSupervision;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseSupervision(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseSupervisionWorkload", OperationKind.Read, true, false,
                    "Shows a supervisor what every analyst who can work a Case Workflow is holding and has done over " +
                    "a period, plus how many open cases nobody holds.",
                    CaseSupervisionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSupervisionStuckLocks", OperationKind.Read, true, false,
                    "Lists open cases that have stayed locked to someone with no activity for a given number of " +
                    "minutes, longest idle first.",
                    CaseSupervisionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSupervisionBreakLock", OperationKind.Write, false, false,
                    "Releases the lock held on an open case so that anyone may take it, recording who broke it and " +
                    "why in the case journal.",
                    CaseSupervisionService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSupervisionReassign", OperationKind.Write, true, false,
                    "Locks an open case to another analyst who can see it, recording the handover in the case " +
                    "journal.",
                    CaseSupervisionService.RequiredPermissions)
            ]);
        }
    }
}

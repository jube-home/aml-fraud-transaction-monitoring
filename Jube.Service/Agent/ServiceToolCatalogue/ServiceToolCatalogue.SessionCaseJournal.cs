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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddSessionCaseJournal(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "SessionCaseJournalGetByCaseWorkflowGuid", OperationKind.Read, Idempotent: true, Destructive: false,
                "Gets the calling user's own saved journal (e.g. case grid column layout) for a CaseWorkflow."),
            new ServiceToolDescriptor(
                "SessionCaseJournalCreate", OperationKind.Write, Idempotent: true, Destructive: false,
                "Saves the calling user's own journal for a CaseWorkflow, replacing any earlier save.")
        ]);
    }
}
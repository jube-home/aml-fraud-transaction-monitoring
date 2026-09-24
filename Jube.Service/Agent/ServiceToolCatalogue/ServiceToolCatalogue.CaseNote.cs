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
        static partial void AddCaseNote(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "CaseNoteInsert", OperationKind.Write, Idempotent: false, Destructive: false,
                "Adds a note to a case, filed against a CaseWorkflowAction; dispatches that action's " +
                "notification/webhook if enabled. Notes are append-only."),
            new ServiceToolDescriptor(
                "CaseNoteValidate", OperationKind.Read, true, false,
                "Validates a Case Note without saving it and returns every failure."),
            new ServiceToolDescriptor(
                "CaseNoteListByCaseKeyValue", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists case notes for a Case Key/Value pair, newest first, scoped to the caller's tenant.")
        ]);
    }
}
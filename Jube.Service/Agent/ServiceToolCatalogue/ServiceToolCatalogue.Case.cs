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
        static partial void AddCase(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "CaseList", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists active cases for the caller's tenant, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "CaseGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Returns a single active case by Id, scoped to the caller's tenant and role."),
            new ServiceToolDescriptor(
                "CaseCreateFromCaseKeyValue", OperationKind.Write, Idempotent: false, Destructive: false,
                "Raises a new case (or promotes an existing one) from a matching archived transaction; " +
                "dispatches the target status's notification/webhook."),
            new ServiceToolDescriptor(
                "CaseUpdate", OperationKind.Write, Idempotent: true, Destructive: false,
                "Updates a case's closed status, lock, diary, rating and workflow status; a status change " +
                "dispatches that status's notification/webhook.")
        ]);
    }
}
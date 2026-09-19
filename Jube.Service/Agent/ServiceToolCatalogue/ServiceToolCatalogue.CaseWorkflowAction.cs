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
        static partial void AddCaseWorkflowAction(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "CaseWorkflowActionList", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists Case Workflow Actions visible to the caller's tenant, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "CaseWorkflowActionListByWorkflowIdActiveOnly", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists active Case Workflow Actions for a parent Case Workflow that the caller holds a role " +
                "grant on."),
            new ServiceToolDescriptor(
                "CaseWorkflowActionListByWorkflowGuidActiveOnly", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists active Case Workflow Actions for a parent Case Workflow (by Guid) that the caller holds " +
                "a role grant on."),
            new ServiceToolDescriptor(
                "CaseWorkflowActionGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Returns a single Case Workflow Action by Id."),
            new ServiceToolDescriptor(
                "CaseWorkflowActionCreate", OperationKind.Write, Idempotent: false, Destructive: false,
                "Creates a Case Workflow Action; calling twice creates two."),
            new ServiceToolDescriptor(
                "CaseWorkflowActionUpdate", OperationKind.Write, Idempotent: true, Destructive: false,
                "Updates a Case Workflow Action; overwrites, writes a version-audit row."),
            new ServiceToolDescriptor(
                "CaseWorkflowActionDelete", OperationKind.Delete, Idempotent: true, Destructive: true,
                "Soft-deletes a Case Workflow Action; reversible only by direct database action.")
        ]);
    }
}
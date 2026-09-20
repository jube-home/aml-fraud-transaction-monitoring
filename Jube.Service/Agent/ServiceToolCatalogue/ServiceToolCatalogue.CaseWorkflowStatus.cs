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
        static partial void AddCaseWorkflowStatus(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "CaseWorkflowStatusList", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists Case Workflow Statuses visible to the caller's tenant, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "CaseWorkflowStatusListByWorkflowGuid", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists Case Workflow Statuses for a parent Case Workflow (by Guid), including inactive ones."),
            new ServiceToolDescriptor(
                "CaseWorkflowStatusListByWorkflowIdActiveOnly", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists active Case Workflow Statuses for a parent Case Workflow that the caller holds a role " +
                "grant on."),
            new ServiceToolDescriptor(
                "CaseWorkflowStatusListByWorkflowGuidActiveOnly", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists active Case Workflow Statuses for a parent Case Workflow (by Guid) that the caller " +
                "holds a role grant on."),
            new ServiceToolDescriptor(
                "CaseWorkflowStatusGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Returns a single Case Workflow Status by Id."),
            new ServiceToolDescriptor(
                "CaseWorkflowStatusCreate", OperationKind.Write, Idempotent: false, Destructive: false,
                "Creates a Case Workflow Status; calling twice creates two."),
            new ServiceToolDescriptor(
                "CaseWorkflowStatusUpdate", OperationKind.Write, Idempotent: true, Destructive: false,
                "Updates a Case Workflow Status; overwrites, writes a version-audit row."),
            new ServiceToolDescriptor(
                "CaseWorkflowStatusDelete", OperationKind.Delete, Idempotent: true, Destructive: true,
                "Soft-deletes a Case Workflow Status; reversible only by direct database action.")
        ]);
    }
}
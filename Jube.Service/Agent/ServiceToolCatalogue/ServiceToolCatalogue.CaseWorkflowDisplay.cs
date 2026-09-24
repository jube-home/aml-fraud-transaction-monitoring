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
        static partial void AddCaseWorkflowDisplay(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayList", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists Case Workflow Displays visible to the caller's tenant, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayFilterFields", OperationKind.Read, true, false,
                "Lists the fields a query builder JSON filter over Case Workflow Displays may use."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayFilter", OperationKind.Read, true, false,
                "Returns the Case Workflow Displays matching query builder JSON, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayCount", OperationKind.Read, true, false,
                "Counts the Case Workflow Displays matching query builder JSON, optionally grouped by a field."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayListByWorkflowIdActiveOnly", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists active Case Workflow Displays for a parent Case Workflow that the caller holds a role " +
                "grant on."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayListByWorkflowGuidActiveOnly", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists active Case Workflow Displays for a parent Case Workflow (by Guid) that the caller holds " +
                "a role grant on."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Returns a single Case Workflow Display by Id."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayCreate", OperationKind.Write, Idempotent: false, Destructive: false,
                "Creates a Case Workflow Display; calling twice creates two."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayValidate", OperationKind.Read, true, false,
                "Validates a Case Workflow Display without saving it and returns every failure."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayUpdate", OperationKind.Write, Idempotent: true, Destructive: false,
                "Updates a Case Workflow Display; overwrites, writes a version-audit row."),
            new ServiceToolDescriptor(
                "CaseWorkflowDisplayDelete", OperationKind.Delete, Idempotent: true, Destructive: true,
                "Soft-deletes a Case Workflow Display; reversible only by direct database action.")
        ]);
    }
}
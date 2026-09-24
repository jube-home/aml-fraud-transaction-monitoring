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
        static partial void AddCaseWorkflow(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "CaseWorkflowList", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists Case Workflows visible to the caller's tenant, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "CaseWorkflowFilterFields", OperationKind.Read, true, false,
                "Lists the fields a query builder JSON filter over Case Workflows may use."),
            new ServiceToolDescriptor(
                "CaseWorkflowFilter", OperationKind.Read, true, false,
                "Returns the Case Workflows matching query builder JSON, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "CaseWorkflowCount", OperationKind.Read, true, false,
                "Counts the Case Workflows matching query builder JSON, optionally grouped by a field."),
            new ServiceToolDescriptor(
                "CaseWorkflowListByModelActiveOnly", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists active Case Workflows for a parent model that the caller holds a role grant on."),
            new ServiceToolDescriptor(
                "CaseWorkflowGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Returns a single Case Workflow by Id."),
            new ServiceToolDescriptor(
                "CaseWorkflowCreate", OperationKind.Write, Idempotent: false, Destructive: false,
                "Creates a Case Workflow; calling twice creates two."),
            new ServiceToolDescriptor(
                "CaseWorkflowValidate", OperationKind.Read, true, false,
                "Validates a Case Workflow without saving it and returns every failure."),
            new ServiceToolDescriptor(
                "CaseWorkflowUpdate", OperationKind.Write, Idempotent: true, Destructive: false,
                "Updates a Case Workflow; overwrites, writes a version-audit row."),
            new ServiceToolDescriptor(
                "CaseWorkflowDelete", OperationKind.Delete, Idempotent: true, Destructive: true,
                "Soft-deletes a Case Workflow; reversible only by direct database action.")
        ]);
    }
}
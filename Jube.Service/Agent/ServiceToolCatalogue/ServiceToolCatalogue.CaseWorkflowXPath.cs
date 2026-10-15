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
using Jube.Service.Repository.CaseWorkflowXPath;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowXPath(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathList", OperationKind.Read, true, false,
                    "Lists Case Workflow XPath extractions visible to the caller's tenant, keyset-paged and capped.",
                    CaseWorkflowXPathService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Case WorkflowXPaths may use.",
                    CaseWorkflowXPathService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathFilter", OperationKind.Read, true, false,
                    "Returns the Case WorkflowXPaths matching query builder JSON, keyset-paged and capped.",
                    CaseWorkflowXPathService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathCount", OperationKind.Read, true, false,
                    "Counts the Case WorkflowXPaths matching query builder JSON, optionally grouped by a field.",
                    CaseWorkflowXPathService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathListByWorkflowIdActiveDrillOnly", OperationKind.Read, true,
                    false,
                    "Lists active, Drill-enabled Case Workflow XPath extractions for a parent Case Workflow that " +
                    "the caller holds a role grant on.",
                    CaseWorkflowXPathService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathListByWorkflowGuidActiveDrillOnly", OperationKind.Read, true,
                    false,
                    "Lists active, Drill-enabled Case Workflow XPath extractions for a parent Case Workflow (by " +
                    "Guid) that the caller holds a role grant on.",
                    CaseWorkflowXPathService.ActiveOnlyPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathGet", OperationKind.Read, true, false,
                    "Returns a single Case Workflow XPath extraction by Id.",
                    CaseWorkflowXPathService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathGetByGuid", OperationKind.Read, true, false,
                    "Returns a single Case Workflow XPath extraction by Id.",
                    CaseWorkflowXPathService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathCreate", OperationKind.Write, false, false,
                    "Creates a Case Workflow XPath extraction; calling twice creates two.",
                    CaseWorkflowXPathService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathValidate", OperationKind.Read, true, false,
                    "Validates a Case WorkflowXPath without saving it and returns every failure.",
                    CaseWorkflowXPathService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathUpdate", OperationKind.Write, true, false,
                    "Updates a Case Workflow XPath extraction; overwrites, writes a version-audit row.",
                    CaseWorkflowXPathService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowXPathDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Case Workflow XPath extraction; reversible only by direct database action.",
                    CaseWorkflowXPathService.RequiredPermissions)
            ]);
        }
    }
}
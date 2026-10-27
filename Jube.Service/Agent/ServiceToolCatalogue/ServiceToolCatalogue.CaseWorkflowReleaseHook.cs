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
using Jube.Service.Repository.CaseWorkflowReleaseHook;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseWorkflowReleaseHook(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseWorkflowReleaseHookList", OperationKind.Read, true, false,
                    "Lists every Case Workflow release hook visible to the caller's tenant.",
                    CaseWorkflowReleaseHookService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowReleaseHookListByCaseWorkflowId", OperationKind.Read, true, false,
                    "Lists the release hooks of a Case Workflow, each chosen by a field of the payload of a held transaction.",
                    CaseWorkflowReleaseHookService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowReleaseHookGet", OperationKind.Read, true, false,
                    "Returns one Case Workflow release hook by Id.",
                    CaseWorkflowReleaseHookService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowReleaseHookCreate", OperationKind.Write, false, false,
                    "Creates the web hook called to release or refuse a payment held on a case, with literal host " +
                    "and tokens only in the path and query; the credential is the name of an environment variable.",
                    CaseWorkflowReleaseHookService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowReleaseHookValidate", OperationKind.Read, true, false,
                    "Runs every check a create or update of a Case Workflow release hook would run, storing nothing.",
                    CaseWorkflowReleaseHookService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowReleaseHookUpdate", OperationKind.Write, true, false,
                    "Updates an unlocked Case Workflow release hook.",
                    CaseWorkflowReleaseHookService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseWorkflowReleaseHookDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an unlocked Case Workflow release hook; reversible only by direct database action.",
                    CaseWorkflowReleaseHookService.RequiredPermissions)
            ]);
        }
    }
}

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
using Jube.Service.EntityAnalysisModelInlineScript;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelInlineScript(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptList", OperationKind.Read, true, false,
                    "Lists Inline Script registrations for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelInlineScriptService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Inline Scripts may use.",
                    EntityAnalysisModelInlineScriptService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptFilter", OperationKind.Read, true, false,
                    "Returns the Inline Scripts matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelInlineScriptService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptCount", OperationKind.Read, true, false,
                    "Counts the Inline Scripts matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelInlineScriptService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptGet", OperationKind.Read, true, false,
                    "Returns one Inline Script registration by id, scoped to the caller's tenant.",
                    EntityAnalysisModelInlineScriptService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptGetByGuid", OperationKind.Read, true, false,
                    "Returns one Inline Script registration by Guid, scoped to the caller's tenant.",
                    EntityAnalysisModelInlineScriptService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false,
                    "Lists Inline Script registrations belonging to a given Model, scoped to the caller's tenant.",
                    EntityAnalysisModelInlineScriptService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptCreate", OperationKind.Write, false, false,
                    "Registers an Inline Script under a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelInlineScriptService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptValidate", OperationKind.Read, true, false,
                    "Validates an Inline Script without saving it and returns every failure.",
                    EntityAnalysisModelInlineScriptService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptExecute", OperationKind.Read, true, false,
                    "Runs a model's inline script against an invocation context when enabled; returns its properties.",
                    EntityAnalysisModelInlineScriptService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptUpdate", OperationKind.Write, true, false,
                    "Updates an Inline Script registration in the caller's tenant by id.",
                    EntityAnalysisModelInlineScriptService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an Inline Script registration in the caller's tenant by id.",
                    EntityAnalysisModelInlineScriptService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected Inline Script registration for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelInlineScriptService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInlineScriptRetract", OperationKind.Write, false, false,
                    "Retracts a rejected Inline Script registration, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelInlineScriptService.WritePermissions)
            ]);
        }
    }
}
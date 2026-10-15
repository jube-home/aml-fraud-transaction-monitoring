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
using Jube.Service.EntityAnalysisModelActivationRuleOverride;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelActivationRuleOverride(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideList", OperationKind.Read, true, false,
                    "Lists Activation-Rule-scoped Overrides for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Activation Rule Overrides may use.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideFilter", OperationKind.Read, true, false,
                    "Returns the Activation Rule Overrides matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideCount", OperationKind.Read, true, false,
                    "Counts the Activation Rule Overrides matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideGet", OperationKind.Read, true, false,
                    "Returns one Activation-Rule-scoped Override by id, scoped to the caller's tenant.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideGetByEntityAnalysisModelGuid", OperationKind.Read,
                    true, false,
                    "Lists Activation-Rule-scoped Overrides belonging to a given Model, scoped to the caller's tenant.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideCreate", OperationKind.Write, false, false,
                    "Registers a Override against a Model/Activation Rule in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideValidate", OperationKind.Read, true, false,
                    "Validates an Activation Rule Override without saving it and returns every failure.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideUpdate", OperationKind.Write, false, false,
                    "Toggles a Override on or off by row existence; NOT idempotent -- repeat calls flip state.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideUpdateDeleteExpiryDate", OperationKind.Write, true,
                    false,
                    "Updates the Delete Expiry Date of an existing active Override in the caller's tenant.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Override in the caller's tenant by id.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleOverrideUpdateOverrideKind", OperationKind.Write, true, false,
                    "Updates the Override Kind of an active Activation Rule Override in the caller's tenant.",
                    EntityAnalysisModelActivationRuleOverrideService.RequiredPermissions)
            ]);
        }
    }
}
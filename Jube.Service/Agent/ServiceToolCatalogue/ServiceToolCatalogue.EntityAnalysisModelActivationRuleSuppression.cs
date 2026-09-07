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
        static partial void AddEntityAnalysisModelActivationRuleSuppression(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleSuppressionList", OperationKind.Read, true, false,
                    "Lists Activation-Rule-scoped Suppressions for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleSuppressionGet", OperationKind.Read, true, false,
                    "Returns one Activation-Rule-scoped Suppression by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleSuppressionGetByEntityAnalysisModelGuid", OperationKind.Read,
                    true, false,
                    "Lists Activation-Rule-scoped Suppressions belonging to a given Model, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleSuppressionCreate", OperationKind.Write, false, false,
                    "Registers a Suppression against a Model/Activation Rule in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleSuppressionUpdate", OperationKind.Write, false, false,
                    "Toggles a Suppression on or off by row existence; NOT idempotent -- repeat calls flip state."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleSuppressionUpdateDeleteExpiryDate", OperationKind.Write, true,
                    false,
                    "Updates the Delete Expiry Date of an existing active Suppression in the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleSuppressionDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Suppression in the caller's tenant by id.")
            ]);
        }
    }
}
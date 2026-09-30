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
        static partial void AddEntityAnalysisModelOverride(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideList", OperationKind.Read, true, false,
                    "Lists Overrides for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Overrides may use."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideFilter", OperationKind.Read, true, false,
                    "Returns the Overrides matching query builder JSON, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideCount", OperationKind.Read, true, false,
                    "Counts the Overrides matching query builder JSON, optionally grouped by a field."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideGet", OperationKind.Read, true, false,
                    "Returns one Override by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideGetByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Lists Overrides belonging to a given Model, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideCreate", OperationKind.Write, false, false,
                    "Registers a Override against a Model in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideValidate", OperationKind.Read, true, false,
                    "Validates a Override without saving it and returns every failure."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideUpdate", OperationKind.Write, false, false,
                    "Toggles a Override on or off by row existence; NOT idempotent -- repeat calls flip state."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideUpdateDeleteExpiryDate", OperationKind.Write, true, false,
                    "Updates the Delete Expiry Date of an existing active Override in the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Override in the caller's tenant by id.")
            ]);
        }
    }
}
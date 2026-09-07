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
        static partial void AddEntityAnalysisModelListValue(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueList", OperationKind.Read, true, false,
                    "Lists List Values for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueGet", OperationKind.Read, true, false,
                    "Returns one List Value matched against its parent List's id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueGetByEntityAnalysisModelListId", OperationKind.Read, true, false,
                    "Lists the values belonging to a given List, ordered by Id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueCreate", OperationKind.Write, false, false,
                    "Registers a value under a List in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueUpdate", OperationKind.Write, true, false,
                    "Updates a value in the caller's tenant by id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a value in the caller's tenant by id.")
            ]);
        }
    }
}
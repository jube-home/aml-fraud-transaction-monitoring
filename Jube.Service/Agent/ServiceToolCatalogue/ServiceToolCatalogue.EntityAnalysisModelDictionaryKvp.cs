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
        static partial void AddEntityAnalysisModelDictionaryKvp(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelDictionaryKvpList", OperationKind.Read, true, false,
                    "Lists Key Value Pairs for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelDictionaryKvpGet", OperationKind.Read, true, false,
                    "Returns one Key Value Pair, matched on the parent Dictionary's id, scoped to the caller's " +
                    "tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelDictionaryKvpGetByEntityAnalysisModelDictionaryId", OperationKind.Read,
                    true, false,
                    "Lists the Key Value Pairs belonging to a given Dictionary, ordered by Id, scoped to the " +
                    "caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelDictionaryKvpCreate", OperationKind.Write, false, false,
                    "Registers a Key Value Pair under a Dictionary in the caller's tenant; calling twice creates " +
                    "two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelDictionaryKvpUpdate", OperationKind.Write, true, false,
                    "Updates a Key Value Pair in the caller's tenant by id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelDictionaryKvpDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Key Value Pair in the caller's tenant by id.")
            ]);
        }
    }
}
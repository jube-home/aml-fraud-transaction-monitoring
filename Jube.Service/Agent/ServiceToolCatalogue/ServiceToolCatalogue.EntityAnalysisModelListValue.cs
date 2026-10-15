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
using Jube.Service.EntityAnalysisModelListValue;

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
                    "Lists List Values for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over List Values may use.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueFilter", OperationKind.Read, true, false,
                    "Returns the List Values matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueCount", OperationKind.Read, true, false,
                    "Counts the List Values matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueGet", OperationKind.Read, true, false,
                    "Returns one List Value matched against its parent List's id, scoped to the caller's tenant.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueGetByGuid", OperationKind.Read, true, false,
                    "Returns one List Value matched against its parent List's id, scoped to the caller's tenant.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueGetByEntityAnalysisModelListId", OperationKind.Read, true, false,
                    "Lists the values belonging to a given List, ordered by Id, scoped to the caller's tenant.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueCreate", OperationKind.Write, false, false,
                    "Registers a value under a List in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueValidate", OperationKind.Read, true, false,
                    "Validates a List Value without saving it and returns every failure.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueUpdate", OperationKind.Write, true, false,
                    "Updates a value in the caller's tenant by id.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a value in the caller's tenant by id.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueUploadCsv", OperationKind.Write, false, false,
                    "Loads one or more CSV files of values into a List in the caller's tenant in a single " +
                    "transaction, recording an upload entry per file; calling twice loads the values twice.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected List Value for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelListValueService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValueRetract", OperationKind.Write, false, false,
                    "Retracts a rejected List Value, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelListValueService.RequiredPermissions)
            ]);
        }
    }
}
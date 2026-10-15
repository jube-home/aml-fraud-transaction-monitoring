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
using Jube.Service.Query.EntityAnalysisModelOverrideQuery;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelOverrideQuery(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideQueryGet", OperationKind.Read, true, false,
                    "Lists tenant models supporting override on a key, flagging active overrides for a key value.",
                    EntityAnalysisModelOverrideQueryService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideQueryGetKeys", OperationKind.Read, true, false,
                    "Lists the override keys available for a Model in the caller's tenant.",
                    EntityAnalysisModelOverrideQueryService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelOverrideQueryGetValues", OperationKind.Read, true, false,
                    "Lists the values held against an override key for a Model in the caller's tenant.",
                    EntityAnalysisModelOverrideQueryService.RequiredPermissions)
            ]);
        }
    }
}
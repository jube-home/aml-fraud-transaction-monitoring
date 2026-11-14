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
using Jube.Service.Repository.CaseTag;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseTag(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseTagListByCaseId", OperationKind.Read, true, false,
                    "Lists the tags applied to a case; case-level tags seed the graph's proximity-to-tag distances.",
                    CaseTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseTagChoices", OperationKind.Read, true, false,
                    "Lists the tag names a case may carry, from its model's tag vocabulary.",
                    CaseTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseTagMerge", OperationKind.Write, true, false,
                    "Replaces the tags on a case with the given list; every name must be in the model's vocabulary.",
                    CaseTagService.RequiredPermissions)
            ]);
        }
    }
}

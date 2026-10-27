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
using Jube.Service.Repository.CaseSearchFilter;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseSearchFilter(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseSearchFilterList", OperationKind.Read, true, false,
                    "Lists the searches saved for the Cases page that the caller owns or that are shared with " +
                    "the caller's role, optionally for one Case Workflow.",
                    CaseSearchFilterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSearchFilterGet", OperationKind.Read, true, false,
                    "Returns one saved Cases page search by Guid.",
                    CaseSearchFilterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSearchFilterCreate", OperationKind.Write, false, false,
                    "Saves a new Cases page search for the caller against a Case Workflow, private unless shared " +
                    "with a role.",
                    CaseSearchFilterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSearchFilterValidate", OperationKind.Read, true, false,
                    "Validates a saved Cases page search without storing it and returns every failure.",
                    CaseSearchFilterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSearchFilterUpdate", OperationKind.Write, true, false,
                    "Changes one of the caller's own saved Cases page searches.",
                    CaseSearchFilterService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSearchFilterDelete", OperationKind.Delete, true, true,
                    "Permanently deletes one of the caller's own saved Cases page searches.",
                    CaseSearchFilterService.RequiredPermissions)
            ]);
        }
    }
}

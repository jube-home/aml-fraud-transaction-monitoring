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
using Jube.Service.Query.CaseSearch;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseSearch(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseSearchRun", OperationKind.Read, true, false,
                    "Lists the cases of a Case Workflow for triage, with each case's risk, why it needs the caller " +
                    "again and its service level position, in lane and Priority order with returned cases lifted.",
                    CaseSearchService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSearchCounts", OperationKind.Read, true, false,
                    "Counts the cases behind every entry of the Cases page rail: the built-in views, the Case " +
                    "Workflow Filters and the caller's saved searches.",
                    CaseSearchService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSearchRiskMatrix", OperationKind.Read, true, false,
                    "Returns the likelihood by impact grid of the cases a search matches.",
                    CaseSearchService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseSearchSkim", OperationKind.Write, false, false,
                    "Takes the next case for the caller in triage order and locks it to them, returned cases first.",
                    CaseSearchService.RequiredPermissions)
            ]);
        }
    }
}

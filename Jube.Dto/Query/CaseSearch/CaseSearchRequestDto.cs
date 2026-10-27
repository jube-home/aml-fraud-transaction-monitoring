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

using System.ComponentModel;
using Jube.Dto.Options;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSearch
{
    [Description("Which cases to list and how. Give the Case Workflow and at most one of a built-in view, a Case " +
                 "Workflow Filter, a saved search or a compiled search; with none, every open case is listed.")]
    public class CaseSearchRequestDto
    {
        [Description("Guid of the Case Workflow whose cases are searched. Required.")]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("A built-in view: 1 My cases, 2 Returned to me, 3 Free for all, 4 Urgent, 5 Overdue, 6 All open.")]
        public CaseSearchView? View { get; set; }

        [Description("Guid of a Case Workflow Filter an administrator defined and the caller's role is granted.")]
        public Guid? CaseWorkflowFilterGuid { get; set; }

        [Description("Guid of a search the caller saved, or one shared with their role.")]
        public Guid? CaseSearchFilterGuid { get; set; }

        [Description("Guid of a compiled Session Case Search the caller made earlier.")]
        public Guid? SessionGuid { get; set; }

        [Description("Order of the list: 0 Triage (live money and mandatory first, then returned cases, then by " +
                     "lane and Priority), 1 Priority, 2 Oldest first, 3 Next deadline, 4 Value, 5 Likelihood, " +
                     "6 Newest first.")]
        public CaseSearchSort Sort { get; set; } = CaseSearchSort.Triage;

        [Description("Page size from 1 to 200. Defaults to 50.")]
        public int Take { get; set; } = 50;

        [Description("How many cases to skip.")]
        public int Skip { get; set; }
    }
}

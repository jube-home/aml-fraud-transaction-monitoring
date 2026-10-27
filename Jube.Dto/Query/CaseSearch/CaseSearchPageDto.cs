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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSearch
{
    [Description("A page of the triage list.")]
    public class CaseSearchPageDto
    {
        [Description("How many cases the search matches in all.")]
        public long Total { get; set; }

        [Description("The page size asked for.")]
        public int Take { get; set; }

        [Description("How many cases were skipped.")]
        public int Skip { get; set; }

        [Description("The order applied.")]
        public int Sort { get; set; }

        [Description("When the page was read (UTC).")]
        public DateTimeOffset GeneratedAt { get; set; }

        [Description("The cases of the page, in order.")]
        public List<CaseSearchRowDto> Rows { get; set; } = [];
    }
}

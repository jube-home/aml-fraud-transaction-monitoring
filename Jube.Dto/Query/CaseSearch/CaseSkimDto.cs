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
    [Description("The case a skim took, and the ones queued behind it.")]
    public class CaseSkimDto
    {
        [Description("The case now locked to the caller; null when nothing was available.")]
        public CaseSearchRowDto? Case { get; set; }

        [Description("Identifiers of the next cases in triage order, to prefetch.")]
        public List<int> NextCaseIds { get; set; } = [];

        [Description("How many unlocked or caller-held cases remain, including the one taken.")]
        public long Remaining { get; set; }
    }
}

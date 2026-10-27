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
using Jube.Dto.Query.CaseSearch;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSummary
{
    [Description("What happened to earlier cases on the same case key.")]
    public class CaseKeyHistoryDto
    {
        [Description("Earlier closed cases on this key that carry an outcome.")]
        public int Closed { get; set; }

        [Description("Of those, how many were confirmed true positives.")]
        public int Confirmed { get; set; }

        [Description("When the newest of them closed (UTC).")]
        public DateTimeOffset? NewestClosedDate { get; set; }

        [Description("Other open cases on the same key and workflow.")]
        public int OtherOpenCases { get; set; }
    }
}

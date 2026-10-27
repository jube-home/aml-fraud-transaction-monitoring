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

namespace Jube.Dto.Query.CaseDiary
{
    [Description("A diary placed on an open case: what it is for and when it falls due.")]
    public class CaseDiarySetDto
    {
        [Description("Identifier of the open case. Required.")]
        public int CaseId { get; set; }

        [Description("What the diary is for: 1 Review, 2 Customer response, 3 Regulatory deadline, 4 Payment hold. " +
                     "A regulatory deadline puts the case in the Mandatory lane and a payment hold in the Live " +
                     "money lane. Required.")]
        public int DiaryTypeId { get; set; }

        [Description("When the diary falls due (UTC). Required. It may be up to a year in the past, to log a " +
                     "deadline that has already been missed, and up to ten years ahead.")]
        public DateTimeOffset? DueDate { get; set; }
    }
}

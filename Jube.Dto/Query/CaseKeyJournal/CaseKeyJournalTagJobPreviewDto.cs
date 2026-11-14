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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseKeyJournal
{
    [Description("How many transactions a journal view matches, so that a bulk tagging can be confirmed first.")]
    [MessagePackObject]
    public class CaseKeyJournalTagJobPreviewDto
    {
        [Description("How many transactions match. When TooMany is true this is the most that can be counted.")]
        [Key(0)]
        public int Matching { get; set; }

        [Description("The most transactions one job will tag.")]
        [Key(1)]
        public int Limit { get; set; }

        [Description("True when more transactions match than a job will tag, so the view must be narrowed.")]
        [Key(2)]
        public bool TooMany { get; set; }
    }
}

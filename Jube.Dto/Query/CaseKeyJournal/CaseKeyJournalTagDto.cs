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

namespace Jube.Dto.Query.CaseKeyJournal
{
    [Description("What a tagging changed: the Tag sets before and after, for the transactions that were changed.")]
    public class CaseKeyJournalTagDto
    {
        [Description("The Tag set each changed transaction carried before. Send these back to restore them.")]
        public CaseKeyJournalTagSetDto[] Previous { get; set; } = [];

        [Description("The Tag set each changed transaction carries now.")]
        public CaseKeyJournalTagSetDto[] Current { get; set; } = [];

        [Description("How many of the requested transactions were not changed because they are not in the " +
                     "workflow's model or no longer exist.")]
        public int Skipped { get; set; }

        [Description("How many of the requested transactions were not changed because their case is locked to " +
                     "another user.")]
        public int Locked { get; set; }
    }
}

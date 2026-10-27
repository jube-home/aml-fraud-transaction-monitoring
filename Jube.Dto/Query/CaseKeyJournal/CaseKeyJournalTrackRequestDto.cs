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
    [Description("A journal view whose transactions are to be drawn on a map, in time order, to look for movement " +
                 "that could not have happened.")]
    public class CaseKeyJournalTrackRequestDto : CaseKeyJournalTagJobPreviewRequestDto
    {
        [Description("When true only the summary of the track is returned and not its points.")]
        public bool? SummaryOnly { get; set; }

        [Description("The case being worked on, so that a configured bold line matches against the case's own " +
                     "transaction.")]
        public int? CaseId { get; set; }

        [Description("When true and a filter is given, every transaction of the view is returned as a point with " +
                     "a Matched flag, so the ones the filter picks out can be drawn among the rest. The movement " +
                     "summary and the counts still cover only the matching transactions.")]
        public bool? IncludeUnmatched { get; set; }

        [Description("The speed in kilometres an hour above which a leg is impossible. Defaults to 900. Between 1 " +
                     "and 10,000.")]
        public double? LimitKmh { get; set; }

        [Description("The shortest leg in kilometres that can be impossible, so that a short hop between two nearby " +
                     "positions is never flagged. Defaults to 50. Between 0 and 20,000.")]
        public double? MinimumKilometres { get; set; }
    }
}

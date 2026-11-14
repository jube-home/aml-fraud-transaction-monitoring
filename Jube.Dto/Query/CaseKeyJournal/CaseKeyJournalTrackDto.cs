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
    [Description("One movement between two consecutive located transactions of a lane.")]
    [MessagePackObject]
    public class CaseKeyJournalTravelLegDto
    {
        [Key(0)] public string Lane { get; set; } = "A";
        [Key(1)] public Guid FromTransaction { get; set; }
        [Key(2)] public Guid ToTransaction { get; set; }
        [Key(3)] public DateTimeOffset FromDate { get; set; }
        [Key(4)] public DateTimeOffset ToDate { get; set; }
        [Key(5)] public double Kilometres { get; set; }
        [Key(6)] public double Hours { get; set; }

        [Description("Kilometres an hour needed to make the leg. Null when both transactions happened at the same " +
                     "instant, which no speed can explain.")]
        [Key(7)]
        public double? SpeedKmh { get; set; }

        [Description("True when the leg is at least the minimum distance and faster than the speed limit.")]
        [Key(8)]
        public bool Impossible { get; set; }
    }

    [Description("What the movement of the track comes to.")]
    [MessagePackObject]
    public class CaseKeyJournalTravelSummaryDto
    {
        [Key(0)] public int Legs { get; set; }
        [Key(1)] public int Impossible { get; set; }
        [Key(2)] public double LimitKmh { get; set; }
        [Key(3)] public double MinimumKilometres { get; set; }

        [Description("The fastest impossible legs, fastest first, at most five.")]
        [Key(4)]
        public List<CaseKeyJournalTravelLegDto> Worst { get; set; } = [];
    }

    [Description("What was asked and what was found.")]
    [MessagePackObject]
    public class CaseKeyJournalTrackMetaDto
    {
        [Key(0)] public string Key { get; set; } = string.Empty;
        [Key(1)] public string Value { get; set; } = string.Empty;
        [Key(2)] public DateTimeOffset From { get; set; }
        [Key(3)] public DateTimeOffset To { get; set; }

        [Description("Transactions of the view read, before the filter.")]
        [Key(4)]
        public int Total { get; set; }

        [Description("Transactions that passed the filter.")]
        [Key(5)]
        public int Matched { get; set; }

        [Description("Matched transactions that have a usable location.")]
        [Key(6)]
        public int Located { get; set; }

        [Description("Matched transactions with no usable location, which are not drawn.")]
        [Key(7)]
        public int Unlocated { get; set; }

        [Description("The most points a track returns.")]
        [Key(8)]
        public int Limit { get; set; }

        [Description("True when the view had more transactions than could be read, so the oldest are left out.")]
        [Key(9)]
        public bool Truncated { get; set; }

        [Description("The payload columns holding latitude and longitude, or null when the caller cannot see both.")]
        [Key(10)]
        public CaseKeyJournalLocationDto? Location { get; set; }
    }

    [Description(
        "The transactions of a journal view in time order with their location, and the movement between them.")]
    [MessagePackObject]
    public class CaseKeyJournalTrackDto
    {
        [Key(0)] public CaseKeyJournalTrackMetaDto Meta { get; set; } = new();
        [Key(1)] public CaseKeyJournalTravelSummaryDto Summary { get; set; } = new();

        [Description("One entry per matched transaction: Id, EntityAnalysisModelInstanceEntryGuid, ReferenceDate, " +
                     "ResponseElevation, Activations, Pinned, Lane, Latitude, Longitude (null when not located), and " +
                     "the BoldLine and CellFormat highlights.")]
        [Key(2)]
        public List<Dictionary<string, object?>> Points { get; set; } = [];
    }
}
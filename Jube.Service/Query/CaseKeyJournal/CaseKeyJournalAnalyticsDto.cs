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
using Jube.CaseRisk.Journal;
using MessagePack;

namespace Jube.Service.Query.CaseKeyJournal
{
    [Description("A field the analytics worked with.")]
    [MessagePackObject]
    public class JournalAnalyticsFieldDto
    {
        [Key(0)]
        public string Name { get; set; } = string.Empty;
        [Key(1)]
        public string Title { get; set; } = string.Empty;
        [Key(2)]
        public string Lens { get; set; } = string.Empty;
        [Key(3)]
        public string Group { get; set; } = string.Empty;
        [Key(4)]
        public int RoleId { get; set; }
        [Key(5)]
        public bool RoleInferred { get; set; }

        [Description("measure, category or counterparty.")]
        [Key(6)]
        public string Kind { get; set; } = "measure";
    }

    [Description("How the case key compares with the other case keys of the workflow (its peers), field by field. " +
                 "Only available once the jobs server has built the peer distributions.")]
    [MessagePackObject]
    public class JournalPeersDto
    {
        [Description("True when at least one field could be compared with its peers.")]
        [Key(0)]
        public bool Available { get; set; }

        [Description("Why peers are not available: NotComputed when no peer distributions have been built for the " +
                     "workflow, NoMeasures when none of the key's fields has one.")]
        [Key(1)]
        public string? Reason { get; set; }

        [Description("When the newest of the distributions used was built (UTC).")]
        [Key(2)]
        public DateTimeOffset? ComputedDate { get; set; }

        [Description("How many days of transactions the distributions were built from.")]
        [Key(3)]
        public int? WindowDays { get; set; }

        [Description("Name of the Case Workflow XPath that splits the peers into segments, when the workflow has one.")]
        [Key(4)]
        public string? Segment { get; set; }

        [Description("The key's value of the segment field, whose peers it was compared with.")]
        [Key(5)]
        public string? SegmentValue { get; set; }

        [Description("True when there were no peers in the key's segment and all keys of the workflow were used.")]
        [Key(6)]
        public bool SegmentFallback { get; set; }

        [Description("One comparison for each field, the most unusual first.")]
        [Key(7)]
        public IReadOnlyList<PeerComparison> Comparisons { get; set; } = [];
    }

    [Description("What was analysed.")]
    [MessagePackObject]
    public class JournalAnalyticsMetaDto
    {
        [Key(0)]
        public string Key { get; set; } = string.Empty;
        [Key(1)]
        public string Value { get; set; } = string.Empty;
        [Key(2)]
        public Guid CaseWorkflowGuid { get; set; }
        [Key(3)]
        public int RowCount { get; set; }
        [Key(4)]
        public int Limit { get; set; }
        [Key(5)]
        public bool Truncated { get; set; }
        [Key(6)]
        public DateTimeOffset From { get; set; }
        [Key(7)]
        public DateTimeOffset To { get; set; }

        [Description("True when there are too few transactions for the statistics to be relied on.")]
        [Key(8)]
        public bool LowConfidence { get; set; }

        [Key(9)]
        public List<string> Sections { get; set; } = [];
    }

    [Description("Statistics computed by the endpoint over the history of a case key. Only the sections asked " +
                 "for are present.")]
    [MessagePackObject]
    public class CaseKeyJournalAnalyticsDto
    {
        [Key(0)]
        public JournalAnalyticsMetaDto Meta { get; set; } = new();
        [Key(1)]
        public List<JournalAnalyticsFieldDto> Fields { get; set; } = [];
        [Key(2)]
        public JournalSummary? Summary { get; set; }
        [Key(3)]
        public NotableReport? Notable { get; set; }
        [Key(4)]
        public ProgressionReport? Progression { get; set; }
        [Key(5)]
        public IReadOnlyList<DistributionReport>? Distributions { get; set; }
        [Key(6)]
        public VelocityReport? Velocity { get; set; }
        [Key(7)]
        public GapReport? Gaps { get; set; }
        [Key(8)]
        public NoveltyReport? Novelty { get; set; }
        [Key(9)]
        public IReadOnlyList<CounterpartyProfile>? Counterparties { get; set; }
        [Key(10)]
        public CorrelationReport? Correlation { get; set; }
        [Key(11)]
        public DriftReport? Drift { get; set; }
        [Key(12)]
        public JournalPeersDto? Peers { get; set; }
    }
}

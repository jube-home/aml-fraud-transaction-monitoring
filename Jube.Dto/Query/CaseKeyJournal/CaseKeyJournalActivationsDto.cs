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
    [Description("One of the inputs that moved a score, signed so that a negative value made the score lower.")]
    [MessagePackObject]
    public class JournalActivationDriverDto
    {
        [Key(0)]
        public string Label { get; set; } = string.Empty;

        [Description("The contribution in the units of the contribution space, negative when it lowered the score.")]
        [Key(1)]
        public double Signed { get; set; }
    }

    [Description("A value a rule read, taken from the anchor transaction.")]
    [MessagePackObject]
    public class JournalActivationPathDto
    {
        [Description("payload, counter, abstraction, calculation, dictionary, sanction or list.")]
        [Key(0)]
        public string Group { get; set; } = string.Empty;

        [Key(1)]
        public string Name { get; set; } = string.Empty;
        [Key(2)]
        public object? Value { get; set; }
    }

    [Description("What fired across the transactions of a journal view, for one rule or one model, with the " +
                 "transaction it is anchored on.")]
    [MessagePackObject]
    public class JournalActivationCardDto
    {
        [Description("rule for an activation rule, model for an exhaustive, advanced analytics or HTTP adaptation " +
                     "model.")]
        [Key(0)]
        public string Kind { get; set; } = string.Empty;

        [Description("For a model, exhaustive, advanced or http. Empty for a rule.")]
        [Key(1)]
        public string? Source { get; set; }

        [Key(2)]
        public string Name { get; set; } = string.Empty;
        [Key(3)]
        public string? HumanLabel { get; set; }

        [Description("How many transactions of the view it fired on.")]
        [Key(4)]
        public int Fired { get; set; }

        [Description("True when an override forced it on the anchor transaction.")]
        [Key(5)]
        public bool Forced { get; set; }

        [Description("The archive id of the latest transaction it fired on.")]
        [Key(6)]
        public long AnchorId { get; set; }

        [Key(7)]
        public DateTimeOffset? AnchorDate { get; set; }

        [Description("For a model, the score on the anchor transaction.")]
        [Key(8)]
        public double? Value { get; set; }

        [Key(9)]
        public double? Threshold { get; set; }

        [Description("True when the score is a calibrated probability, the only score that may be called one.")]
        [Key(10)]
        public bool Calibrated { get; set; }

        [Description("For a calibrated probability, the band its value falls in: 1 Unlikely, 2 Possible, 3 Likely, " +
                     "4 Very likely.")]
        [Key(11)]
        public int? BandId { get; set; }

        [Description("For a calibrated model, how often the reliability band holding the score was observed true.")]
        [Key(12)]
        public double? ObservedRate { get; set; }

        [Key(13)]
        public double? ExpectedRate { get; set; }
        [Key(14)]
        public int? BandSample { get; set; }

        [Description("The model name and version as it scored, when the engine recorded them.")]
        [Key(15)]
        public string? ModelName { get; set; }

        [Key(16)]
        public string? ModelVersion { get; set; }

        [Description("The largest contributions, up to four, with negative ones always kept.")]
        [Key(17)]
        public List<JournalActivationDriverDto> Drivers { get; set; } = [];

        [Description("How many further visible inputs contributed.")]
        [Key(18)]
        public int OtherDrivers { get; set; }

        [Description("For a rule, the counters, abstractions, fields and other entities its text reads, with the " +
                     "values they had on the anchor transaction.")]
        [Key(19)]
        public List<JournalActivationPathDto> Path { get; set; } = [];

        [Description("For a rule, how many inputs the caller's roles may not see.")]
        [Key(20)]
        public int HiddenPath { get; set; }

        [Description("True when the rule matched in shadow mode on the anchor transaction, so had no effect.")]
        [Key(21)]
        public bool Shadow { get; set; }
    }

    [Description("The scope an activations list was drawn from.")]
    [MessagePackObject]
    public class JournalActivationsMetaDto
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
    }

    [Description("Every rule and model that fired across the transactions of a journal view.")]
    [MessagePackObject]
    public class CaseKeyJournalActivationsDto
    {
        [Key(0)]
        public JournalActivationsMetaDto Meta { get; set; } = new();
        [Key(1)]
        public List<JournalActivationCardDto> Cards { get; set; } = [];
    }
}

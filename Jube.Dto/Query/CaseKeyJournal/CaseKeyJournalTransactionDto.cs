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
using Jube.Dto.Hold;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseKeyJournal
{
    [Description("An activation that fired on the transaction.")]
    [MessagePackObject]
    public class JournalTransactionActivationDto
    {
        [Key(0)]
        public string Name { get; set; } = string.Empty;

        [Description("True when the activation was forced by an override.")]
        [Key(1)]
        public bool Forced { get; set; }

        [Description("True when the activation matched in shadow mode and so had no effect.")]
        [Key(2)]
        public bool Shadow { get; set; }
    }

    [Description("A value the rules or models saw, from one facet of the archived transaction.")]
    [MessagePackObject]
    public class JournalTransactionValueDto
    {
        [Description("abstraction, calculation, counter, dictionary, sanction or http.")]
        [Key(0)]
        public string Group { get; set; } = string.Empty;

        [Key(1)]
        public string Name { get; set; } = string.Empty;
        [Key(2)]
        public object? Value { get; set; }
    }

    [Description("An identifier on the transaction the journal can be pivoted to.")]
    [MessagePackObject]
    public class JournalTransactionKeyDto
    {
        [Key(0)]
        public string Name { get; set; } = string.Empty;
        [Key(1)]
        public string? Value { get; set; }
        [Key(2)]
        public bool Drill { get; set; }

        [Description("The payload field to pivot the journal on, such as 'Device', or empty when the identifier " +
                     "does not come straight from the payload and cannot be pivoted to.")]
        [Key(3)]
        public string? PayloadKey { get; set; }

        [Description("The data type of the field behind the identifier: 1 String, 2 Integer, 3 Float, 4 Date, " +
                     "5 Boolean, 6 Latitude, 7 Longitude. Numeric and date keys may be merged as a range.")]
        [Key(4)]
        public int DataTypeId { get; set; }
    }

    [Description("The model that produced an adaptation, as it was when it scored.")]
    [MessagePackObject]
    public class JournalAdaptationModelDto
    {
        [Key(0)]
        public string? Name { get; set; }
        [Key(1)]
        public string? Family { get; set; }
        [Key(2)]
        public string? Version { get; set; }
        [Key(3)]
        public string? ArtifactHash { get; set; }
        [Key(4)]
        public DateTimeOffset? TrainedDate { get; set; }
        [Key(5)]
        public int? FeatureCount { get; set; }
    }

    [Description("Whether the score crossed the model's threshold.")]
    [MessagePackObject]
    public class JournalAdaptationResultDto
    {
        [Key(0)]
        public double? Threshold { get; set; }
        [Key(1)]
        public bool? Activated { get; set; }
        [Key(2)]
        public double? ExpectedPositiveRate { get; set; }
    }

    [Description("How well the score was calibrated, with its reliability bands.")]
    [MessagePackObject]
    public class JournalCalibrationBandDto
    {
        [Key(0)]
        public double? Lower { get; set; }
        [Key(1)]
        public double? Upper { get; set; }
        [Key(2)]
        public double? Expected { get; set; }
        [Key(3)]
        public double? Observed { get; set; }
        [Key(4)]
        public int? Count { get; set; }
    }

    [MessagePackObject]
    public class JournalCalibrationDto
    {
        [Key(0)]
        public string? Space { get; set; }
        [Key(1)]
        public bool? Calibrated { get; set; }
        [Key(2)]
        public string? Method { get; set; }
        [Key(3)]
        public DateTimeOffset? ValidatedDate { get; set; }
        [Key(4)]
        public int? Sample { get; set; }
        [Key(5)]
        public double? Brier { get; set; }
        [Key(6)]
        public double? Intercept { get; set; }
        [Key(7)]
        public double? Slope { get; set; }
        [Key(8)]
        public List<JournalCalibrationBandDto> Bands { get; set; } = [];
    }

    [Description("One input's contribution to the score.")]
    [MessagePackObject]
    public class JournalContributionItemDto
    {
        [Key(0)]
        public string? Name { get; set; }
        [Key(1)]
        public double? Weight { get; set; }

        [Description("Sign of the contribution: positive pushes the score up, negative down.")]
        [Key(2)]
        public double? Direction { get; set; }

        [Key(3)]
        public double? Significance { get; set; }
        [Key(4)]
        public string? Source { get; set; }
        [Key(5)]
        public string? HumanLabel { get; set; }

        [Description("The contribution with its sign applied, which is what the waterfall plots.")]
        [Key(6)]
        public double? Signed { get; set; }

        [Description("True when the input was hidden because the caller's roles may not see it. Hidden inputs are " +
                     "never named and never add up to a figure.")]
        [Key(7)]
        public bool Hidden { get; set; }
    }

    [MessagePackObject]
    public class JournalContributionDto
    {
        [Key(0)]
        public string? Space { get; set; }
        [Key(1)]
        public string? Method { get; set; }

        [Description("True when the contributions add up exactly to the score from the base value.")]
        [Key(2)]
        public bool? Exact { get; set; }

        [Key(3)]
        public double? BaseValue { get; set; }
        [Key(4)]
        public List<JournalContributionItemDto> Items { get; set; } = [];
        [Key(5)]
        public int HiddenItems { get; set; }
    }

    [Description("One step of the decision path the model took.")]
    [MessagePackObject]
    public class JournalJourneyNodeDto
    {
        [Key(0)]
        public string? Feature { get; set; }
        [Key(1)]
        public string? Operator { get; set; }
        [Key(2)]
        public double? Threshold { get; set; }
        [Key(3)]
        public string? ThresholdCategory { get; set; }
        [Key(4)]
        public string? Source { get; set; }
        [Key(5)]
        public string? HumanLabel { get; set; }

        [Description("True when the input was hidden because the caller's roles may not see it.")]
        [Key(6)]
        public bool Hidden { get; set; }
    }

    [Description("What a model produced for the transaction, with the full account of how.")]
    [MessagePackObject]
    public class JournalAdaptationDto
    {
        [Description("exhaustive, advanced or http.")]
        [Key(0)]
        public string Source { get; set; } = string.Empty;

        [Key(1)]
        public string Name { get; set; } = string.Empty;
        [Key(2)]
        public double? Value { get; set; }
        [Key(3)]
        public string? Error { get; set; }
        [Key(4)]
        public string? HumanLabel { get; set; }
        [Key(5)]
        public string? Narrative { get; set; }
        [Key(6)]
        public JournalAdaptationModelDto? Model { get; set; }
        [Key(7)]
        public JournalAdaptationResultDto? Result { get; set; }
        [Key(8)]
        public JournalCalibrationDto? Calibration { get; set; }
        [Key(9)]
        public JournalContributionDto? Contribution { get; set; }

        [Description("The decision path, empty when the model has no path, as only tree and rule models do.")]
        [Key(10)]
        public List<JournalJourneyNodeDto> Journey { get; set; } = [];
    }

    [Description("The archived transaction as the model and the rules saw it, with every model's decision journey " +
                 "and contributions. Read-only.")]
    [MessagePackObject]
    public class CaseKeyJournalTransactionDto
    {
        [Key(0)]
        public long Id { get; set; }
        [Key(1)]
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [Key(2)]
        public DateTimeOffset? ReferenceDate { get; set; }
        [Key(3)]
        public DateTimeOffset? CreatedDate { get; set; }
        [Key(4)]
        public double ResponseElevation { get; set; }

        [Description("The name of the response elevation range the elevation fell inside, when it fell inside one.")]
        [Key(5)]
        public string? ResponseElevationName { get; set; }

        [Description("The hold stamped on the transaction when its response elevation range held or declined it: " +
                     "the state, the range, who released or refused it and why, and every message sent about it. " +
                     "Null when nothing was held. Read-only here: a held payment is released or refused from the " +
                     "case.")]
        [Key(6)]
        public ResponseHold? Hold { get; set; }

        [Key(7)]
        public string? EntryKeyValue { get; set; }
        [Key(8)]
        public string? ModelName { get; set; }
        [Key(9)]
        public List<string> Tags { get; set; } = [];
        [Key(10)]
        public List<JournalTransactionActivationDto> Activations { get; set; } = [];
        [Key(11)]
        public List<JournalTransactionValueDto> Values { get; set; } = [];
        [Key(12)]
        public List<JournalTransactionKeyDto> Keys { get; set; } = [];
        [Key(13)]
        public List<JournalAdaptationDto> Adaptations { get; set; } = [];
    }
}

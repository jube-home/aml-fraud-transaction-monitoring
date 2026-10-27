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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseKeyJournal
{
    [Description("An activation that fired on the transaction.")]
    public class JournalTransactionActivationDto
    {
        public string Name { get; set; } = string.Empty;

        [Description("True when the activation was forced by an override.")]
        public bool Forced { get; set; }
    }

    [Description("A value the rules or models saw, from one facet of the archived transaction.")]
    public class JournalTransactionValueDto
    {
        [Description("abstraction, calculation, counter, dictionary, sanction or http.")]
        public string Group { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    [Description("An identifier on the transaction the journal can be pivoted to.")]
    public class JournalTransactionKeyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
        public bool Drill { get; set; }

        [Description("The payload field to pivot the journal on, such as 'Device', or empty when the identifier " +
                     "does not come straight from the payload and cannot be pivoted to.")]
        public string? PayloadKey { get; set; }
    }

    [Description("The model that produced an adaptation, as it was when it scored.")]
    public class JournalAdaptationModelDto
    {
        public string? Name { get; set; }
        public string? Family { get; set; }
        public string? Version { get; set; }
        public string? ArtifactHash { get; set; }
        public DateTimeOffset? TrainedDate { get; set; }
        public int? FeatureCount { get; set; }
    }

    [Description("Whether the score crossed the model's threshold.")]
    public class JournalAdaptationResultDto
    {
        public double? Threshold { get; set; }
        public bool? Activated { get; set; }
        public double? ExpectedPositiveRate { get; set; }
    }

    [Description("How well the score was calibrated, with its reliability bands.")]
    public class JournalCalibrationBandDto
    {
        public double? Lower { get; set; }
        public double? Upper { get; set; }
        public double? Expected { get; set; }
        public double? Observed { get; set; }
        public int? Count { get; set; }
    }

    public class JournalCalibrationDto
    {
        public string? Space { get; set; }
        public bool? Calibrated { get; set; }
        public string? Method { get; set; }
        public DateTimeOffset? ValidatedDate { get; set; }
        public int? Sample { get; set; }
        public double? Brier { get; set; }
        public double? Intercept { get; set; }
        public double? Slope { get; set; }
        public List<JournalCalibrationBandDto> Bands { get; set; } = [];
    }

    [Description("One input's contribution to the score.")]
    public class JournalContributionItemDto
    {
        public string? Name { get; set; }
        public double? Weight { get; set; }

        [Description("Sign of the contribution: positive pushes the score up, negative down.")]
        public double? Direction { get; set; }

        public double? Significance { get; set; }
        public string? Source { get; set; }
        public string? HumanLabel { get; set; }

        [Description("The contribution with its sign applied, which is what the waterfall plots.")]
        public double? Signed { get; set; }

        [Description("True when the input was hidden because the caller's roles may not see it. Hidden inputs are " +
                     "never named and never add up to a figure.")]
        public bool Hidden { get; set; }
    }

    public class JournalContributionDto
    {
        public string? Space { get; set; }
        public string? Method { get; set; }

        [Description("True when the contributions add up exactly to the score from the base value.")]
        public bool? Exact { get; set; }

        public double? BaseValue { get; set; }
        public List<JournalContributionItemDto> Items { get; set; } = [];
        public int HiddenItems { get; set; }
    }

    [Description("One step of the decision path the model took.")]
    public class JournalJourneyNodeDto
    {
        public string? Feature { get; set; }
        public string? Operator { get; set; }
        public double? Threshold { get; set; }
        public string? ThresholdCategory { get; set; }
        public string? Source { get; set; }
        public string? HumanLabel { get; set; }

        [Description("True when the input was hidden because the caller's roles may not see it.")]
        public bool Hidden { get; set; }
    }

    [Description("What a model produced for the transaction, with the full account of how.")]
    public class JournalAdaptationDto
    {
        [Description("exhaustive, advanced or http.")]
        public string Source { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public double? Value { get; set; }
        public string? Error { get; set; }
        public string? HumanLabel { get; set; }
        public string? Narrative { get; set; }
        public JournalAdaptationModelDto? Model { get; set; }
        public JournalAdaptationResultDto? Result { get; set; }
        public JournalCalibrationDto? Calibration { get; set; }
        public JournalContributionDto? Contribution { get; set; }

        [Description("The decision path, empty when the model has no path, as only tree and rule models do.")]
        public List<JournalJourneyNodeDto> Journey { get; set; } = [];
    }

    [Description("The archived transaction as the model and the rules saw it, with every model's decision journey " +
                 "and contributions. Read-only.")]
    public class CaseKeyJournalTransactionDto
    {
        public long Id { get; set; }
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        public DateTimeOffset? ReferenceDate { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public double ResponseElevation { get; set; }

        [Description("The name of the response elevation range the elevation fell inside, when it fell inside one.")]
        public string? ResponseElevationName { get; set; }

        [Description("The hold stamped on the transaction when its response elevation range held or declined it: " +
                     "the state, the range, who released or refused it and why, and every message sent about it. " +
                     "Null when nothing was held. Read-only here: a held payment is released or refused from the " +
                     "case.")]
        public ResponseHold? Hold { get; set; }

        public string? EntryKeyValue { get; set; }
        public string? ModelName { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<JournalTransactionActivationDto> Activations { get; set; } = [];
        public List<JournalTransactionValueDto> Values { get; set; } = [];
        public List<JournalTransactionKeyDto> Keys { get; set; } = [];
        public List<JournalAdaptationDto> Adaptations { get; set; } = [];
    }
}

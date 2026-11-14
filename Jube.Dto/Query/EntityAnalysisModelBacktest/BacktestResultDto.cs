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
using Jube.Dto.Validation;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.EntityAnalysisModelBacktest
{
    [Description("Raw counts from running a rule over archived transactions: how many were read, how many the " +
                 "filter kept, how often the rule fired and, with a class, the four cells of the confusion matrix. " +
                 "No rates are worked out.")]
    [MessagePackObject]
    public class BacktestResultDto
    {
        [Description("True when the request was valid and everything compiled, so the counts are meaningful.")]
        [Key(0)]
        public bool Valid { get; set; }

        [Description("How many archived transactions were read.")]
        [Key(1)]
        public long Scanned { get; set; }

        [Description("How many the filter left out, including those it raised an error on.")]
        [Key(2)]
        public long FilteredOut { get; set; }

        [Description("How many the filter raised an error on.")]
        [Key(3)]
        public long FilterErrors { get; set; }

        [Description("How many the rule ran over (those the filter kept).")]
        [Key(4)]
        public long Evaluated { get; set; }

        [Description("True when Limit transactions were read, so older ones in the range were not.")]
        [Key(5)]
        public bool LimitReached { get; set; }

        [Description("The earliest reference date among the transactions evaluated.")]
        [Key(6)]
        public DateTime? EarliestReferenceDate { get; set; }

        [Description("The latest reference date among the transactions evaluated.")]
        [Key(7)]
        public DateTime? LatestReferenceDate { get; set; }

        [Description("How many transactions the rule fired on.")]
        [Key(8)]
        public long Fired { get; set; }

        [Description("How many it did not fire on, including those where it raised an error, as the engine treats " +
                     "an error as not matched.")]
        [Key(9)]
        public long NotFired { get; set; }

        [Description("How many transactions the rule raised an error on.")]
        [Key(10)]
        public long RuntimeErrors { get; set; }

        [Description("True when the backtest stopped early; AbortReason says why.")]
        [Key(11)]
        public bool Aborted { get; set; }

        [Description("Why the backtest stopped early.")]
        [Key(12)]
        public string? AbortReason { get; set; }

        [Description("True when a class was given, so the confusion matrix is filled.")]
        [Key(13)]
        public bool ClassDefined { get; set; }

        [Description("How many transactions were in the positive class.")]
        [Key(14)]
        public long Positives { get; set; }

        [Description("Fired and positive.")] [Key(15)] public long TruePositives { get; set; }

        [Description("Fired and not positive.")]
        [Key(16)]
        public long FalsePositives { get; set; }

        [Description("Did not fire but positive.")]
        [Key(17)]
        public long FalseNegatives { get; set; }

        [Description("Did not fire and not positive.")]
        [Key(18)]
        public long TrueNegatives { get; set; }

        [Description("Total time spent running the filter and the rule.")]
        [Key(19)]
        public long DurationMicroseconds { get; set; }

        [Description("Every tag found on the transactions evaluated, most common first.")]
        [Key(20)]
        public List<BacktestTagCountDto> TagsInSample { get; set; } = [];

        [Description("Examples fired on and positive.")]
        [Key(21)]
        public List<BacktestSampleDto> TruePositiveSamples { get; set; } = [];

        [Description("Examples fired on and not positive.")]
        [Key(22)]
        public List<BacktestSampleDto> FalsePositiveSamples { get; set; } = [];

        [Description("Examples positive and not fired on.")]
        [Key(23)]
        public List<BacktestSampleDto> FalseNegativeSamples { get; set; } = [];

        [Description("Examples the rule raised an error on.")]
        [Key(24)]
        public List<BacktestSampleDto> ErrorSamples { get; set; } = [];

        [Description("Why the request could not be run, each with its property (RuleText, Filter…, Class…) and, " +
                     "for rule text, the line and position. Empty when Valid is true.")]
        [Key(25)]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}
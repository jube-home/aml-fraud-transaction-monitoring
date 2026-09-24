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

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Query.EntityAnalysisModelBacktest
{
    [Description("Raw counts from running a rule over archived transactions: how many were read, how many the " +
                 "filter kept, how often the rule fired and, with a class, the four cells of the confusion matrix. " +
                 "No rates are worked out.")]
    public class BacktestResultDto
    {
        [Description("True when the request was valid and everything compiled, so the counts are meaningful.")]
        public bool Valid { get; set; }

        [Description("How many archived transactions were read.")]
        public long Scanned { get; set; }

        [Description("How many the filter left out, including those it raised an error on.")]
        public long FilteredOut { get; set; }

        [Description("How many the filter raised an error on.")]
        public long FilterErrors { get; set; }

        [Description("How many the rule ran over (those the filter kept).")]
        public long Evaluated { get; set; }

        [Description("True when Limit transactions were read, so older ones in the range were not.")]
        public bool LimitReached { get; set; }

        [Description("The earliest reference date among the transactions evaluated.")]
        public DateTime? EarliestReferenceDate { get; set; }

        [Description("The latest reference date among the transactions evaluated.")]
        public DateTime? LatestReferenceDate { get; set; }

        [Description("How many transactions the rule fired on.")]
        public long Fired { get; set; }

        [Description("How many it did not fire on, including those where it raised an error, as the engine treats " +
                     "an error as not matched.")]
        public long NotFired { get; set; }

        [Description("How many transactions the rule raised an error on.")]
        public long RuntimeErrors { get; set; }

        [Description("True when the backtest stopped early; AbortReason says why.")]
        public bool Aborted { get; set; }

        [Description("Why the backtest stopped early.")]
        public string? AbortReason { get; set; }

        [Description("True when a class was given, so the confusion matrix is filled.")]
        public bool ClassDefined { get; set; }

        [Description("How many transactions were in the positive class.")]
        public long Positives { get; set; }

        [Description("Fired and positive.")] public long TruePositives { get; set; }

        [Description("Fired and not positive.")]
        public long FalsePositives { get; set; }

        [Description("Did not fire but positive.")]
        public long FalseNegatives { get; set; }

        [Description("Did not fire and not positive.")]
        public long TrueNegatives { get; set; }

        [Description("Total time spent running the filter and the rule.")]
        public long DurationMicroseconds { get; set; }

        [Description("Every tag found on the transactions evaluated, most common first.")]
        public List<BacktestTagCountDto> TagsInSample { get; set; } = [];

        [Description("Examples fired on and positive.")]
        public List<BacktestSampleDto> TruePositiveSamples { get; set; } = [];

        [Description("Examples fired on and not positive.")]
        public List<BacktestSampleDto> FalsePositiveSamples { get; set; } = [];

        [Description("Examples positive and not fired on.")]
        public List<BacktestSampleDto> FalseNegativeSamples { get; set; } = [];

        [Description("Examples the rule raised an error on.")]
        public List<BacktestSampleDto> ErrorSamples { get; set; } = [];

        [Description("Why the request could not be run, each with its property (RuleText, Filter…, Class…) and, " +
                     "for rule text, the line and position. Empty when Valid is true.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}
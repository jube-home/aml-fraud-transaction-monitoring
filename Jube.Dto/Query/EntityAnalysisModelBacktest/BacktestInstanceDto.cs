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

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Query.EntityAnalysisModelBacktest
{
    [Description("A submitted backtest and where it has got to.")]
    public class BacktestInstanceDto
    {
        [Description("The instance's id; poll it with EntityAnalysisModelBacktestStatus.")]
        public int Id { get; set; }

        [Description("The instance's Guid.")] public Guid Guid { get; set; }

        [Description("The model.")] public int EntityAnalysisModelId { get; set; }

        [Description("The kind of rule tested.")]
        public string RuleType { get; set; } = string.Empty;

        [Description("The saved rule it was started from.")]
        public int? RuleId { get; set; }

        [Description("Pending, Running, Succeeded, Failed, Cancelling or Cancelled.")]
        public string Status { get; set; } = string.Empty;

        [Description("How far through its Limit it has read, from 0 to 1.")]
        public double Progress { get; set; }

        [Description("How many archived transactions it has read.")]
        public long Scanned { get; set; }

        [Description("How many the rule has run over.")]
        public long Evaluated { get; set; }

        [Description("When it was submitted.")]
        public DateTime CreatedDate { get; set; }

        [Description("Who submitted it.")] public string? CreatedUser { get; set; }

        [Description("When a backtest thread started it.")]
        public DateTime? StartedDate { get; set; }

        [Description("When it finished.")] public DateTime? CompletedDate { get; set; }

        [Description("Why it failed or was cancelled.")]
        public string? Error { get; set; }

        [Description("What it runs: the filter, rule, class, range and limit.")]
        public BacktestRequestDto Request { get; set; } = new();

        [Description("When finished, how often the rule fired.")]
        public long? Fired { get; set; }

        [Description("When finished with a class, how many transactions were positive.")]
        public long? Positives { get; set; }

        [Description("When finished with a class, fired and positive.")]
        public long? TruePositives { get; set; }

        [Description("When finished with a class, fired and not positive.")]
        public long? FalsePositives { get; set; }

        [Description("When finished with a class, positive and not fired.")]
        public long? FalseNegatives { get; set; }

        [Description("When finished with a class, neither.")]
        public long? TrueNegatives { get; set; }
    }
}
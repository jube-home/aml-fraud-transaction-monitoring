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

namespace Jube.Dto.Query.EntityAnalysisModelBacktest
{
    [Description("A submitted backtest and where it has got to.")]
    [MessagePackObject]
    public class BacktestInstanceDto
    {
        [Description("The instance's id; poll it with EntityAnalysisModelBacktestStatus.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("The instance's Guid.")] [Key(1)] public Guid Guid { get; set; }
        [Description("The model.")] [Key(2)] public int EntityAnalysisModelId { get; set; }

        [Description("The kind of rule tested.")]
        [Key(3)]
        public string RuleType { get; set; } = string.Empty;

        [Description("The saved rule it was started from.")]
        [Key(4)]
        public int? RuleId { get; set; }

        [Description("Pending, Running, Succeeded, Failed, Cancelling or Cancelled.")]
        [Key(5)]
        public string Status { get; set; } = string.Empty;

        [Description("How far through its Limit it has read, from 0 to 1.")]
        [Key(6)]
        public double Progress { get; set; }

        [Description("How many archived transactions it has read.")]
        [Key(7)]
        public long Scanned { get; set; }

        [Description("How many the rule has run over.")]
        [Key(8)]
        public long Evaluated { get; set; }

        [Description("When it was submitted.")]
        [Key(9)]
        public DateTime CreatedDate { get; set; }

        [Description("Who submitted it.")] [Key(10)] public string? CreatedUser { get; set; }

        [Description("When a backtest thread started it.")]
        [Key(11)]
        public DateTime? StartedDate { get; set; }

        [Description("When it finished.")] [Key(12)] public DateTime? CompletedDate { get; set; }

        [Description("Why it failed or was cancelled.")]
        [Key(13)]
        public string? Error { get; set; }

        [Description("What it runs: the filter, rule, class, range and limit.")]
        [Key(14)]
        public BacktestRequestDto Request { get; set; } = new();

        [Description("When finished, how often the rule fired.")]
        [Key(15)]
        public long? Fired { get; set; }

        [Description("When finished with a class, how many transactions were positive.")]
        [Key(16)]
        public long? Positives { get; set; }

        [Description("When finished with a class, fired and positive.")]
        [Key(17)]
        public long? TruePositives { get; set; }

        [Description("When finished with a class, fired and not positive.")]
        [Key(18)]
        public long? FalsePositives { get; set; }

        [Description("When finished with a class, positive and not fired.")]
        [Key(19)]
        public long? FalseNegatives { get; set; }

        [Description("When finished with a class, neither.")]
        [Key(20)]
        public long? TrueNegatives { get; set; }
    }
}
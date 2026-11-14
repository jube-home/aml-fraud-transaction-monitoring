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
    [Description("The outcome of one candidate value in a tuning sweep.")]
    [MessagePackObject]
    public class BacktestTuneVariantDto
    {
        [Description("The candidate value.")] [Key(0)] public string Value { get; set; } = string.Empty;

        [Description("How many transactions the rule ran over.")] [Key(1)] public long Evaluated { get; set; }

        [Description("How many the rule fired on.")] [Key(2)] public long Fired { get; set; }

        [Description("How many the rule raised an error on.")] [Key(3)] public long RuntimeErrors { get; set; }

        [Description("Fired and positive.")] [Key(4)] public long TruePositives { get; set; }

        [Description("Fired and not positive.")] [Key(5)] public long FalsePositives { get; set; }

        [Description("Did not fire but positive.")] [Key(6)] public long FalseNegatives { get; set; }

        [Description("Did not fire and not positive.")] [Key(7)] public long TrueNegatives { get; set; }

        [Description("The share of firings that were positive; empty when the rule never fired.")]
        [Key(8)]
        public double? Precision { get; set; }

        [Description("The share of positives the rule fired on; empty when there were no positives.")]
        [Key(9)]
        public double? Recall { get; set; }
    }
}

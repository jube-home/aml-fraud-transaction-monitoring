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
    [Description("The outcome of a tuning sweep: one variant per candidate, in the order given, or why the sweep was refused.")]
    [MessagePackObject]
    public class BacktestTuneResultDto
    {
        [Description("True when every candidate ran.")] [Key(0)] public bool Valid { get; set; }

        [Description("The variants, one per distinct candidate.")]
        [Key(1)]
        public List<BacktestTuneVariantDto> Variants { get; set; } = [];

        [Description("Why the sweep was refused, as for a backtest.")]
        [Key(2)]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}

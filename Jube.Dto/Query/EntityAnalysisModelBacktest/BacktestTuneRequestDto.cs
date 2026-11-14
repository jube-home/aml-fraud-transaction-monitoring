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
    [Description("A tuning sweep: a base backtest whose RuleText carries a placeholder, and the candidate numbers to put in its place.")]
    [MessagePackObject]
    public class BacktestTuneRequestDto
    {
        [Description("The base backtest. Its RuleText must contain the Placeholder; set ClassJson to the outcome that counts as positive.")]
        [Key(0)]
        public BacktestRequestDto? Request { get; set; }

        [Description("The text in RuleText to replace with each candidate, for example {{threshold}}.")]
        [Key(1)]
        public string Placeholder { get; set; } = string.Empty;

        [Description("The candidate values, each a plain number written with a full stop for the decimal point; at most 12.")]
        [Key(2)]
        public List<string> Candidates { get; set; } = [];
    }
}

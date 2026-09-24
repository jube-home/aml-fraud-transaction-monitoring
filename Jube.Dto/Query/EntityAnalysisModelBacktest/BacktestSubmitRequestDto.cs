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
    [Description("A backtest to submit, optionally over a calendar window ending now instead of an explicit range.")]
    public class BacktestSubmitRequestDto
    {
        [Description("The backtest request: the filter, rule, class, range and limit.")]
        public BacktestRequestDto? Request { get; set; } = new();

        [Description("The calendar interval of a window ending now, e.g. d for days or m for months; with " +
                     "WindowValue it replaces From and To.")]
        public string? WindowInterval { get; set; }

        [Description("How many WindowInterval units the window covers.")]
        public int? WindowValue { get; set; }
    }
}
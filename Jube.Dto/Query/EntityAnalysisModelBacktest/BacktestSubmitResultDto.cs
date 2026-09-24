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
    [Description("The outcome of submitting a backtest: the instance when the request is valid, otherwise why not.")]
    public class BacktestSubmitResultDto
    {
        [Description("True when the backtest was submitted.")]
        public bool Valid { get; set; }

        [Description("The submitted instance.")]
        public BacktestInstanceDto? Instance { get; set; }

        [Description("Why the request was refused, as for an immediate backtest.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}
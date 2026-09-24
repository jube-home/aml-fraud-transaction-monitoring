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
using Jube.Dto.Filter;

namespace Jube.Dto.Query.EntityAnalysisModelBacktest
{
    [Description("The fields a backtest's filter and class may use.")]
    public class BacktestFieldsDto
    {
        [Description("Fields for FilterJson: those of a reprocessing rule.")]
        public List<FilterFieldDto> FilterFields { get; set; } = [];

        [Description("Fields for ClassJson: everything an activation rule can see, plus Tag.<Name> for each of the " +
                     "model's tags.")]
        public List<FilterFieldDto> ClassFields { get; set; } = [];
    }
}
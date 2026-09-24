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
    [Description("One archived transaction from the backtest; explain it to see why the rule fired or not.")]
    public class BacktestSampleDto
    {
        [Description("The transaction's entry Guid.")]
        public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

        [Description("The transaction's entry key value.")]
        public string? EntryKeyValue { get; set; }

        [Description("The transaction's reference date.")]
        public DateTime? ReferenceDate { get; set; }

        [Description("Whether the rule fired.")]
        public bool Fired { get; set; }

        [Description("Whether the transaction is in the positive class.")]
        public bool Positive { get; set; }

        [Description("The error the rule raised on this transaction, if any.")]
        public string? Error { get; set; }
    }
}
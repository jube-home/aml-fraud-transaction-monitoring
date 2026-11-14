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
using System;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseSuppression
{
    [MessagePackObject]
    public class CaseSuppressionCountsDto
    {
        [Description("Transactions in the case key journal, and merged lanes, that carry the value.")]
        [Key(0)]
        public int CountInCase { get; set; }

        [Description("Earliest of those transactions (UTC).")]
        [Key(1)]
        public DateTimeOffset? FirstSeenDate { get; set; }

        [Description("Latest of those transactions (UTC).")]
        [Key(2)]
        public DateTimeOffset? LastSeenDate { get; set; }

        [Description("Whether the wider count could be made as an integer: 1 Counted, 2 NotIndexed.")]
        [Key(3)]
        public int WiderCountStateId { get; set; }

        [Description("Transactions in the model archive carrying the value, capped. Null when not counted.")]
        [Key(4)]
        public int? WiderTransactions { get; set; }

        [Description("Distinct case keys among them, capped. Null when not counted.")]
        [Key(5)]
        public int? WiderKeys { get; set; }

        [Description("True when the wider count stopped at its cap and the true figure is higher.")]
        [Key(6)]
        public bool WiderCapped { get; set; }
    }
}

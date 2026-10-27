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
using Jube.Dto.Query.CaseSearch;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSummary
{
    [Description("One entry of the audit trail of a case.")]
    public class CaseEventSummaryDto
    {
        [Description("Event type: 1 Automatic Unlock through 16 Peek.")]
        public int EventTypeId { get; set; }

        [Description("English name of the event type.")]
        public string? EventType { get; set; }

        [Description("Who caused it.")]
        public string? User { get; set; }

        [Description("When it happened (UTC).")]
        public DateTimeOffset? Date { get; set; }

        [Description("The value before the change, where the event records one.")]
        public string? Before { get; set; }

        [Description("The value after the change, where the event records one.")]
        public string? After { get; set; }

        [Description("True when the event happened after the caller last looked and someone else caused it.")]
        public bool New { get; set; }
    }
}

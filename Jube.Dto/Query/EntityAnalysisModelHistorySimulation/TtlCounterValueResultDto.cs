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

namespace Jube.Dto.Query.EntityAnalysisModelHistorySimulation
{
    [Description("The value a TTL counter would have for the transaction in a context, read from the cache as " +
                 "the engine reads it. Nothing is stored or incremented.")]
    public class TtlCounterValueResultDto
    {
        [Description("True when the value was read; when false, Errors says why.")]
        public bool Read { get; set; }

        [Description("Why the value was not read, e.g. the counter's data field has no value in the context.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];

        [Description("The TTL counter's name, as used in rules (TTLCounter.Name).")]
        public string Name { get; set; } = string.Empty;

        [Description("The payload field the counter is kept per, e.g. AccountId.")]
        public string DataName { get; set; } = string.Empty;

        [Description("The data field's value taken from the context.")]
        public string? DataValue { get; set; }

        [Description("True when the counter is aggregated online over a window rather than read as a running " +
                     "count.")]
        public bool OnlineAggregation { get; set; }

        [Description("For online aggregation, the start of the window, in UTC; otherwise null.")]
        public DateTime? WindowFrom { get; set; }

        [Description("The counter value, as invariant text.")]
        public string? Value { get; set; }
    }
}
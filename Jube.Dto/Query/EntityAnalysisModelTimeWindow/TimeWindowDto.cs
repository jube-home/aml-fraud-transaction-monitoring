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

namespace Jube.Dto.Query.EntityAnalysisModelTimeWindow
{
    [Description("A time window worked out exactly as the engine works it out. Both ends are included.")]
    public class TimeWindowDto
    {
        [Description("True when the window could be worked out.")]
        public bool Valid { get; set; }

        [Description("The window's kind: AbstractionRule, SearchKey, TtlCounter or Calendar.")]
        public string? Kind { get; set; }

        [Description("The interval code: s, n, h, d, w, m or y.")]
        public string? Interval { get; set; }

        [Description("The interval in words, e.g. minutes for n.")]
        public string? IntervalName { get; set; }

        [Description("How many intervals the window spans.")]
        public int Value { get; set; }

        [Description("The start of the window, included.")]
        public DateTime? From { get; set; }

        [Description("The end of the window (the reference date), included.")]
        public DateTime? To { get; set; }

        [Description("The window's length in seconds.")]
        public double? LengthSeconds { get; set; }

        [Description("For an abstraction rule, true when the search key's TTL is shorter than the rule's interval, " +
                     "so the engine uses the shorter window.")]
        public bool SearchKeyShortensTheWindow { get; set; }

        [Description("For a TTL counter set to live forever: its entries never expire, so it has no window.")]
        public bool LiveForever { get; set; }

        [Description("How the engine uses this window.")]
        public string? Note { get; set; }

        [Description("Why the window could not be worked out.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}
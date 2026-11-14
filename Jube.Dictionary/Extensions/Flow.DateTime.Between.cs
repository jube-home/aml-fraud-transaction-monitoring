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

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(Flow<DateTime> flow)
        {
            [Description("True when: strictly between the two dates. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }

            [Description("True when: strictly between the two dates. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }

            [Description("True when: strictly between the two dates. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }

            [Description("True when: strictly between the two dates. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }

            [Description("True when: strictly between the two dates. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeBetween(flow.Value, rangeStart, rangeEnd));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: strictly between the two dates. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return value.Start().MatchBetween(rangeStart, rangeEnd);
            }

            [Description("Begins a pipeline from the value. True when: strictly between the two dates. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return value.Start().RejectBetween(rangeStart, rangeEnd);
            }

            [Description("Begins a pipeline from the value. True when: strictly between the two dates. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return value.Start().BreakBetween(rangeStart, rangeEnd);
            }

            [Description("Begins a pipeline from the value. True when: strictly between the two dates. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return value.Start().RequireBetween(rangeStart, rangeEnd);
            }

            [Description("Begins a pipeline from the value. True when: strictly between the two dates. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureBetween([Description("The start of the range.")] DateTime rangeStart,
                [Description("The end of the range.")] DateTime rangeEnd)
            {
                return value.Start().EnsureBetween(rangeStart, rangeEnd);
            }
        }
    }
}

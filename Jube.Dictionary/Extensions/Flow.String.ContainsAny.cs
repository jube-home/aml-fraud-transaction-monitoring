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
        extension(Flow<string?> flow)
        {
            [Description("True when: contains at least one of the listed strings. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsAny(flow.Value, values));
            }

            [Description("True when: contains at least one of the listed strings. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsAny(flow.Value, values));
            }

            [Description("True when: contains at least one of the listed strings. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsAny(flow.Value, values));
            }

            [Description("True when: contains at least one of the listed strings. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsAny(flow.Value, values));
            }

            [Description("True when: contains at least one of the listed strings. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsAny(flow.Value, values));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: contains at least one of the listed strings. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return value.Start().MatchContainsAny(values);
            }

            [Description("Begins a pipeline from the value. True when: contains at least one of the listed strings. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return value.Start().RejectContainsAny(values);
            }

            [Description("Begins a pipeline from the value. True when: contains at least one of the listed strings. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return value.Start().BreakContainsAny(values);
            }

            [Description("Begins a pipeline from the value. True when: contains at least one of the listed strings. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return value.Start().RequireContainsAny(values);
            }

            [Description("Begins a pipeline from the value. True when: contains at least one of the listed strings. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsAny([Description("The substrings to search for.")] params string[] values)
            {
                return value.Start().EnsureContainsAny(values);
            }
        }
    }
}

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
            [Description("True when: one of the listed strings, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringInIgnoreCase(flow.Value, values));
            }

            [Description("True when: one of the listed strings, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringInIgnoreCase(flow.Value, values));
            }

            [Description("True when: one of the listed strings, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringInIgnoreCase(flow.Value, values));
            }

            [Description("True when: one of the listed strings, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringInIgnoreCase(flow.Value, values));
            }

            [Description("True when: one of the listed strings, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringInIgnoreCase(flow.Value, values));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: one of the listed strings, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return value.Start().MatchInIgnoreCase(values);
            }

            [Description("Begins a pipeline from the value. True when: one of the listed strings, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return value.Start().RejectInIgnoreCase(values);
            }

            [Description("Begins a pipeline from the value. True when: one of the listed strings, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return value.Start().BreakInIgnoreCase(values);
            }

            [Description("Begins a pipeline from the value. True when: one of the listed strings, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return value.Start().RequireInIgnoreCase(values);
            }

            [Description("Begins a pipeline from the value. True when: one of the listed strings, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureInIgnoreCase([Description("The values to look for.")] params string[] values)
            {
                return value.Start().EnsureInIgnoreCase(values);
            }
        }
    }
}

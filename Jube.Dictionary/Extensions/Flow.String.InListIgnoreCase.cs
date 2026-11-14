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
            [Description("True when: a member of the List token, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringInListIgnoreCase(flow.Value, list));
            }

            [Description("True when: a member of the List token, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringInListIgnoreCase(flow.Value, list));
            }

            [Description("True when: a member of the List token, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringInListIgnoreCase(flow.Value, list));
            }

            [Description("True when: a member of the List token, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringInListIgnoreCase(flow.Value, list));
            }

            [Description("True when: a member of the List token, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringInListIgnoreCase(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: a member of the List token, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchInListIgnoreCase(list);
            }

            [Description("Begins a pipeline from the value. True when: a member of the List token, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectInListIgnoreCase(list);
            }

            [Description("Begins a pipeline from the value. True when: a member of the List token, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakInListIgnoreCase(list);
            }

            [Description("Begins a pipeline from the value. True when: a member of the List token, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireInListIgnoreCase(list);
            }

            [Description("Begins a pipeline from the value. True when: a member of the List token, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureInListIgnoreCase(list);
            }
        }
    }
}

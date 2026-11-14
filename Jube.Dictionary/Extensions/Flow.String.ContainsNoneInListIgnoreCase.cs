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
            [Description("True when: present and contains no entry of the List token, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsNoneInListIgnoreCase(flow.Value, list));
            }

            [Description("True when: present and contains no entry of the List token, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsNoneInListIgnoreCase(flow.Value, list));
            }

            [Description("True when: present and contains no entry of the List token, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsNoneInListIgnoreCase(flow.Value, list));
            }

            [Description("True when: present and contains no entry of the List token, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsNoneInListIgnoreCase(flow.Value, list));
            }

            [Description("True when: present and contains no entry of the List token, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsNoneInListIgnoreCase(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token, ignoring case. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchContainsNoneInListIgnoreCase(list);
            }

            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token, ignoring case. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectContainsNoneInListIgnoreCase(list);
            }

            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token, ignoring case. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakContainsNoneInListIgnoreCase(list);
            }

            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token, ignoring case. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireContainsNoneInListIgnoreCase(list);
            }

            [Description("Begins a pipeline from the value. True when: present and contains no entry of the List token, ignoring case. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsNoneInListIgnoreCase([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureContainsNoneInListIgnoreCase(list);
            }
        }
    }
}

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
            [Description("True when: present and not a member of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringNotInList(flow.Value, list));
            }

            [Description("True when: present and not a member of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringNotInList(flow.Value, list));
            }

            [Description("True when: present and not a member of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringNotInList(flow.Value, list));
            }

            [Description("True when: present and not a member of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringNotInList(flow.Value, list));
            }

            [Description("True when: present and not a member of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringNotInList(flow.Value, list));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: present and not a member of the List token. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().MatchNotInList(list);
            }

            [Description("Begins a pipeline from the value. True when: present and not a member of the List token. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RejectNotInList(list);
            }

            [Description("Begins a pipeline from the value. True when: present and not a member of the List token. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().BreakNotInList(list);
            }

            [Description("Begins a pipeline from the value. True when: present and not a member of the List token. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().RequireNotInList(list);
            }

            [Description("Begins a pipeline from the value. True when: present and not a member of the List token. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureNotInList([Description("The List token whose entries are compared.")] IEnumerable<string> list)
            {
                return value.Start().EnsureNotInList(list);
            }
        }
    }
}

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
            [Description("True when: present but zero length. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsEmpty(flow.Value));
            }

            [Description("True when: present but zero length. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsEmpty(flow.Value));
            }

            [Description("True when: present but zero length. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsEmpty(flow.Value));
            }

            [Description("True when: present but zero length. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsEmpty(flow.Value));
            }

            [Description("True when: present but zero length. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsEmpty()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsEmpty(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: present but zero length. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsEmpty()
            {
                return value.Start().MatchIsEmpty();
            }

            [Description("Begins a pipeline from the value. True when: present but zero length. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsEmpty()
            {
                return value.Start().RejectIsEmpty();
            }

            [Description("Begins a pipeline from the value. True when: present but zero length. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsEmpty()
            {
                return value.Start().BreakIsEmpty();
            }

            [Description("Begins a pipeline from the value. True when: present but zero length. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsEmpty()
            {
                return value.Start().RequireIsEmpty();
            }

            [Description("Begins a pipeline from the value. True when: present but zero length. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsEmpty()
            {
                return value.Start().EnsureIsEmpty();
            }
        }
    }
}

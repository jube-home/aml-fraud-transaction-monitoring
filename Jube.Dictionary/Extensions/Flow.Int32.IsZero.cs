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
        extension(Flow<int> flow)
        {
            [Description("True when: zero. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32IsZero(flow.Value));
            }

            [Description("True when: zero. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32IsZero(flow.Value));
            }

            [Description("True when: zero. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32IsZero(flow.Value));
            }

            [Description("True when: zero. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32IsZero(flow.Value));
            }

            [Description("True when: zero. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32IsZero(flow.Value));
            }
        }

        extension(int value)
        {
            [Description("Begins a pipeline from the value. True when: zero. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIsZero()
            {
                return value.Start().MatchIsZero();
            }

            [Description("Begins a pipeline from the value. True when: zero. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIsZero()
            {
                return value.Start().RejectIsZero();
            }

            [Description("Begins a pipeline from the value. True when: zero. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIsZero()
            {
                return value.Start().BreakIsZero();
            }

            [Description("Begins a pipeline from the value. True when: zero. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIsZero()
            {
                return value.Start().RequireIsZero();
            }

            [Description("Begins a pipeline from the value. True when: zero. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIsZero()
            {
                return value.Start().EnsureIsZero();
            }
        }
    }
}

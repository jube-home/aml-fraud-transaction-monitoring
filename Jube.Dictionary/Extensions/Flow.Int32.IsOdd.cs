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
            [Description("True when: an odd number. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIsOdd()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32IsOdd(flow.Value));
            }

            [Description("True when: an odd number. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIsOdd()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32IsOdd(flow.Value));
            }

            [Description("True when: an odd number. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIsOdd()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32IsOdd(flow.Value));
            }

            [Description("True when: an odd number. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIsOdd()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32IsOdd(flow.Value));
            }

            [Description("True when: an odd number. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIsOdd()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32IsOdd(flow.Value));
            }
        }

        extension(int value)
        {
            [Description("Begins a pipeline from the value. True when: an odd number. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIsOdd()
            {
                return value.Start().MatchIsOdd();
            }

            [Description("Begins a pipeline from the value. True when: an odd number. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIsOdd()
            {
                return value.Start().RejectIsOdd();
            }

            [Description("Begins a pipeline from the value. True when: an odd number. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIsOdd()
            {
                return value.Start().BreakIsOdd();
            }

            [Description("Begins a pipeline from the value. True when: an odd number. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIsOdd()
            {
                return value.Start().RequireIsOdd();
            }

            [Description("Begins a pipeline from the value. True when: an odd number. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIsOdd()
            {
                return value.Start().EnsureIsOdd();
            }
        }
    }
}

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
            [Description("True when: an even number. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIsEven()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32IsEven(flow.Value));
            }

            [Description("True when: an even number. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIsEven()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32IsEven(flow.Value));
            }

            [Description("True when: an even number. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIsEven()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32IsEven(flow.Value));
            }

            [Description("True when: an even number. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIsEven()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32IsEven(flow.Value));
            }

            [Description("True when: an even number. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIsEven()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32IsEven(flow.Value));
            }
        }

        extension(int value)
        {
            [Description("Begins a pipeline from the value. True when: an even number. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIsEven()
            {
                return value.Start().MatchIsEven();
            }

            [Description("Begins a pipeline from the value. True when: an even number. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIsEven()
            {
                return value.Start().RejectIsEven();
            }

            [Description("Begins a pipeline from the value. True when: an even number. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIsEven()
            {
                return value.Start().BreakIsEven();
            }

            [Description("Begins a pipeline from the value. True when: an even number. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIsEven()
            {
                return value.Start().RequireIsEven();
            }

            [Description("Begins a pipeline from the value. True when: an even number. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIsEven()
            {
                return value.Start().EnsureIsEven();
            }
        }
    }
}

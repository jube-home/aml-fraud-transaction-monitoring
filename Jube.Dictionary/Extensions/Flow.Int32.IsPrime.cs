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
            [Description("True when: a prime number. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIsPrime()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32IsPrime(flow.Value));
            }

            [Description("True when: a prime number. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIsPrime()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32IsPrime(flow.Value));
            }

            [Description("True when: a prime number. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIsPrime()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32IsPrime(flow.Value));
            }

            [Description("True when: a prime number. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIsPrime()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32IsPrime(flow.Value));
            }

            [Description("True when: a prime number. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIsPrime()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32IsPrime(flow.Value));
            }
        }

        extension(int value)
        {
            [Description("Begins a pipeline from the value. True when: a prime number. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIsPrime()
            {
                return value.Start().MatchIsPrime();
            }

            [Description("Begins a pipeline from the value. True when: a prime number. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIsPrime()
            {
                return value.Start().RejectIsPrime();
            }

            [Description("Begins a pipeline from the value. True when: a prime number. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIsPrime()
            {
                return value.Start().BreakIsPrime();
            }

            [Description("Begins a pipeline from the value. True when: a prime number. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIsPrime()
            {
                return value.Start().RequireIsPrime();
            }

            [Description("Begins a pipeline from the value. True when: a prime number. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIsPrime()
            {
                return value.Start().EnsureIsPrime();
            }
        }
    }
}

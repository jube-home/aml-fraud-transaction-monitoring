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
        extension(Flow<double> flow)
        {
            public Flow<double> MatchIsMultipleOf(double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }

            public Flow<double> RejectIsMultipleOf(double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }

            public Flow<double> BreakIsMultipleOf(double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }

            public Flow<double> RequireIsMultipleOf(double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }

            public Flow<double> EnsureIsMultipleOf(double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsMultipleOf(double divisor)
            {
                return value.Start().MatchIsMultipleOf(divisor);
            }

            public Flow<double> RejectIsMultipleOf(double divisor)
            {
                return value.Start().RejectIsMultipleOf(divisor);
            }

            public Flow<double> BreakIsMultipleOf(double divisor)
            {
                return value.Start().BreakIsMultipleOf(divisor);
            }

            public Flow<double> RequireIsMultipleOf(double divisor)
            {
                return value.Start().RequireIsMultipleOf(divisor);
            }

            public Flow<double> EnsureIsMultipleOf(double divisor)
            {
                return value.Start().EnsureIsMultipleOf(divisor);
            }
        }
    }
}

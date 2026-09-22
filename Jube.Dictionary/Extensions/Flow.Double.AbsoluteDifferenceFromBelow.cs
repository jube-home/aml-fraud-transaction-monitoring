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
            public Flow<double> MatchAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }

            public Flow<double> RejectAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }

            public Flow<double> BreakAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }

            public Flow<double> RequireAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }

            public Flow<double> EnsureAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return value.Start().MatchAbsoluteDifferenceFromBelow(other, bound);
            }

            public Flow<double> RejectAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return value.Start().RejectAbsoluteDifferenceFromBelow(other, bound);
            }

            public Flow<double> BreakAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return value.Start().BreakAbsoluteDifferenceFromBelow(other, bound);
            }

            public Flow<double> RequireAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return value.Start().RequireAbsoluteDifferenceFromBelow(other, bound);
            }

            public Flow<double> EnsureAbsoluteDifferenceFromBelow(double other, double bound)
            {
                return value.Start().EnsureAbsoluteDifferenceFromBelow(other, bound);
            }
        }
    }
}

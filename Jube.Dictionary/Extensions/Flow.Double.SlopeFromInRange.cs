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
            public Flow<double> MatchSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleSlopeFromInRange(flow.Value, previous, elapsed, lowerBound, upperBound));
            }

            public Flow<double> RejectSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleSlopeFromInRange(flow.Value, previous, elapsed, lowerBound, upperBound));
            }

            public Flow<double> BreakSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleSlopeFromInRange(flow.Value, previous, elapsed, lowerBound, upperBound));
            }

            public Flow<double> RequireSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleSlopeFromInRange(flow.Value, previous, elapsed, lowerBound, upperBound));
            }

            public Flow<double> EnsureSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleSlopeFromInRange(flow.Value, previous, elapsed, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return value.Start().MatchSlopeFromInRange(previous, elapsed, lowerBound, upperBound);
            }

            public Flow<double> RejectSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return value.Start().RejectSlopeFromInRange(previous, elapsed, lowerBound, upperBound);
            }

            public Flow<double> BreakSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return value.Start().BreakSlopeFromInRange(previous, elapsed, lowerBound, upperBound);
            }

            public Flow<double> RequireSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return value.Start().RequireSlopeFromInRange(previous, elapsed, lowerBound, upperBound);
            }

            public Flow<double> EnsureSlopeFromInRange(double previous, double elapsed, double lowerBound, double upperBound)
            {
                return value.Start().EnsureSlopeFromInRange(previous, elapsed, lowerBound, upperBound);
            }
        }
    }
}

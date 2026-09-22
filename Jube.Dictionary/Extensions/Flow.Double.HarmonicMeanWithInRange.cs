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
            public Flow<double> MatchHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RejectHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> BreakHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RequireHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> EnsureHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().MatchHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }

            public Flow<double> RejectHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RejectHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }

            public Flow<double> BreakHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().BreakHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }

            public Flow<double> RequireHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RequireHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }

            public Flow<double> EnsureHarmonicMeanWithInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().EnsureHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }
        }
    }
}

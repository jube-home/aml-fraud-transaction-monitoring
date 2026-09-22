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
            public Flow<double> MatchPercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentBelowThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }

            public Flow<double> RejectPercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentBelowThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }

            public Flow<double> BreakPercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentBelowThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }

            public Flow<double> RequirePercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentBelowThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }

            public Flow<double> EnsurePercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentBelowThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchPercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return value.Start().MatchPercentBelowThresholdInRange(threshold, lowerBound, upperBound);
            }

            public Flow<double> RejectPercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return value.Start().RejectPercentBelowThresholdInRange(threshold, lowerBound, upperBound);
            }

            public Flow<double> BreakPercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return value.Start().BreakPercentBelowThresholdInRange(threshold, lowerBound, upperBound);
            }

            public Flow<double> RequirePercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return value.Start().RequirePercentBelowThresholdInRange(threshold, lowerBound, upperBound);
            }

            public Flow<double> EnsurePercentBelowThresholdInRange(double threshold, double lowerBound, double upperBound)
            {
                return value.Start().EnsurePercentBelowThresholdInRange(threshold, lowerBound, upperBound);
            }
        }
    }
}

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
            public Flow<double> MatchMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleMeanWithInRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> RejectMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleMeanWithInRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> BreakMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleMeanWithInRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> RequireMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleMeanWithInRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> EnsureMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleMeanWithInRange(flow.Value, lowerBound, upperBound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().MatchMeanWithInRange(lowerBound, upperBound, others);
            }

            public Flow<double> RejectMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().RejectMeanWithInRange(lowerBound, upperBound, others);
            }

            public Flow<double> BreakMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().BreakMeanWithInRange(lowerBound, upperBound, others);
            }

            public Flow<double> RequireMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().RequireMeanWithInRange(lowerBound, upperBound, others);
            }

            public Flow<double> EnsureMeanWithInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().EnsureMeanWithInRange(lowerBound, upperBound, others);
            }
        }
    }
}

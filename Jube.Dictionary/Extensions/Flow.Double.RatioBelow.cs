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
            public Flow<double> MatchRatioBelow(double denominator, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRatioBelow(flow.Value, denominator, threshold));
            }

            public Flow<double> RejectRatioBelow(double denominator, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRatioBelow(flow.Value, denominator, threshold));
            }

            public Flow<double> BreakRatioBelow(double denominator, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRatioBelow(flow.Value, denominator, threshold));
            }

            public Flow<double> RequireRatioBelow(double denominator, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRatioBelow(flow.Value, denominator, threshold));
            }

            public Flow<double> EnsureRatioBelow(double denominator, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRatioBelow(flow.Value, denominator, threshold));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchRatioBelow(double denominator, double threshold)
            {
                return value.Start().MatchRatioBelow(denominator, threshold);
            }

            public Flow<double> RejectRatioBelow(double denominator, double threshold)
            {
                return value.Start().RejectRatioBelow(denominator, threshold);
            }

            public Flow<double> BreakRatioBelow(double denominator, double threshold)
            {
                return value.Start().BreakRatioBelow(denominator, threshold);
            }

            public Flow<double> RequireRatioBelow(double denominator, double threshold)
            {
                return value.Start().RequireRatioBelow(denominator, threshold);
            }

            public Flow<double> EnsureRatioBelow(double denominator, double threshold)
            {
                return value.Start().EnsureRatioBelow(denominator, threshold);
            }
        }
    }
}

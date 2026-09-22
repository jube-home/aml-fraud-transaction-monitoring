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
            public Flow<double> MatchPercentBelowThresholdAbove(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentBelowThresholdAbove(flow.Value, threshold, bound));
            }

            public Flow<double> RejectPercentBelowThresholdAbove(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentBelowThresholdAbove(flow.Value, threshold, bound));
            }

            public Flow<double> BreakPercentBelowThresholdAbove(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentBelowThresholdAbove(flow.Value, threshold, bound));
            }

            public Flow<double> RequirePercentBelowThresholdAbove(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentBelowThresholdAbove(flow.Value, threshold, bound));
            }

            public Flow<double> EnsurePercentBelowThresholdAbove(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentBelowThresholdAbove(flow.Value, threshold, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchPercentBelowThresholdAbove(double threshold, double bound)
            {
                return value.Start().MatchPercentBelowThresholdAbove(threshold, bound);
            }

            public Flow<double> RejectPercentBelowThresholdAbove(double threshold, double bound)
            {
                return value.Start().RejectPercentBelowThresholdAbove(threshold, bound);
            }

            public Flow<double> BreakPercentBelowThresholdAbove(double threshold, double bound)
            {
                return value.Start().BreakPercentBelowThresholdAbove(threshold, bound);
            }

            public Flow<double> RequirePercentBelowThresholdAbove(double threshold, double bound)
            {
                return value.Start().RequirePercentBelowThresholdAbove(threshold, bound);
            }

            public Flow<double> EnsurePercentBelowThresholdAbove(double threshold, double bound)
            {
                return value.Start().EnsurePercentBelowThresholdAbove(threshold, bound);
            }
        }
    }
}

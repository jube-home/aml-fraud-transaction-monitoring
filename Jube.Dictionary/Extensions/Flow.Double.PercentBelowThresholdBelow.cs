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
            public Flow<double> MatchPercentBelowThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentBelowThresholdBelow(flow.Value, threshold, bound));
            }

            public Flow<double> RejectPercentBelowThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentBelowThresholdBelow(flow.Value, threshold, bound));
            }

            public Flow<double> BreakPercentBelowThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentBelowThresholdBelow(flow.Value, threshold, bound));
            }

            public Flow<double> RequirePercentBelowThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentBelowThresholdBelow(flow.Value, threshold, bound));
            }

            public Flow<double> EnsurePercentBelowThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentBelowThresholdBelow(flow.Value, threshold, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchPercentBelowThresholdBelow(double threshold, double bound)
            {
                return value.Start().MatchPercentBelowThresholdBelow(threshold, bound);
            }

            public Flow<double> RejectPercentBelowThresholdBelow(double threshold, double bound)
            {
                return value.Start().RejectPercentBelowThresholdBelow(threshold, bound);
            }

            public Flow<double> BreakPercentBelowThresholdBelow(double threshold, double bound)
            {
                return value.Start().BreakPercentBelowThresholdBelow(threshold, bound);
            }

            public Flow<double> RequirePercentBelowThresholdBelow(double threshold, double bound)
            {
                return value.Start().RequirePercentBelowThresholdBelow(threshold, bound);
            }

            public Flow<double> EnsurePercentBelowThresholdBelow(double threshold, double bound)
            {
                return value.Start().EnsurePercentBelowThresholdBelow(threshold, bound);
            }
        }
    }
}

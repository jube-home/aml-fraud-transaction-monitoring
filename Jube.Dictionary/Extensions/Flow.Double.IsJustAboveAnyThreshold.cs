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
            public Flow<double> MatchIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsJustAboveAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            public Flow<double> RejectIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsJustAboveAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            public Flow<double> BreakIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsJustAboveAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            public Flow<double> RequireIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsJustAboveAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            public Flow<double> EnsureIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsJustAboveAnyThreshold(flow.Value, marginPercent, thresholds));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().MatchIsJustAboveAnyThreshold(marginPercent, thresholds);
            }

            public Flow<double> RejectIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().RejectIsJustAboveAnyThreshold(marginPercent, thresholds);
            }

            public Flow<double> BreakIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().BreakIsJustAboveAnyThreshold(marginPercent, thresholds);
            }

            public Flow<double> RequireIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().RequireIsJustAboveAnyThreshold(marginPercent, thresholds);
            }

            public Flow<double> EnsureIsJustAboveAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().EnsureIsJustAboveAnyThreshold(marginPercent, thresholds);
            }
        }
    }
}

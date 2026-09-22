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
            public Flow<double> MatchIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            public Flow<double> RejectIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            public Flow<double> BreakIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            public Flow<double> RequireIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            public Flow<double> EnsureIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().MatchIsJustBelowAnyThreshold(marginPercent, thresholds);
            }

            public Flow<double> RejectIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().RejectIsJustBelowAnyThreshold(marginPercent, thresholds);
            }

            public Flow<double> BreakIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().BreakIsJustBelowAnyThreshold(marginPercent, thresholds);
            }

            public Flow<double> RequireIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().RequireIsJustBelowAnyThreshold(marginPercent, thresholds);
            }

            public Flow<double> EnsureIsJustBelowAnyThreshold(double marginPercent, params double[] thresholds)
            {
                return value.Start().EnsureIsJustBelowAnyThreshold(marginPercent, thresholds);
            }
        }
    }
}

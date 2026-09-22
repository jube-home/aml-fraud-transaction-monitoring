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
            public Flow<double> MatchIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }

            public Flow<double> RejectIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }

            public Flow<double> BreakIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }

            public Flow<double> RequireIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }

            public Flow<double> EnsureIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return value.Start().MatchIsJustBelowThreshold(threshold, marginPercent);
            }

            public Flow<double> RejectIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return value.Start().RejectIsJustBelowThreshold(threshold, marginPercent);
            }

            public Flow<double> BreakIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return value.Start().BreakIsJustBelowThreshold(threshold, marginPercent);
            }

            public Flow<double> RequireIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return value.Start().RequireIsJustBelowThreshold(threshold, marginPercent);
            }

            public Flow<double> EnsureIsJustBelowThreshold(double threshold, double marginPercent)
            {
                return value.Start().EnsureIsJustBelowThreshold(threshold, marginPercent);
            }
        }
    }
}

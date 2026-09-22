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
            public Flow<double> MatchInverseRatioOfAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleInverseRatioOfAbove(flow.Value, other, bound));
            }

            public Flow<double> RejectInverseRatioOfAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleInverseRatioOfAbove(flow.Value, other, bound));
            }

            public Flow<double> BreakInverseRatioOfAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleInverseRatioOfAbove(flow.Value, other, bound));
            }

            public Flow<double> RequireInverseRatioOfAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleInverseRatioOfAbove(flow.Value, other, bound));
            }

            public Flow<double> EnsureInverseRatioOfAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleInverseRatioOfAbove(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchInverseRatioOfAbove(double other, double bound)
            {
                return value.Start().MatchInverseRatioOfAbove(other, bound);
            }

            public Flow<double> RejectInverseRatioOfAbove(double other, double bound)
            {
                return value.Start().RejectInverseRatioOfAbove(other, bound);
            }

            public Flow<double> BreakInverseRatioOfAbove(double other, double bound)
            {
                return value.Start().BreakInverseRatioOfAbove(other, bound);
            }

            public Flow<double> RequireInverseRatioOfAbove(double other, double bound)
            {
                return value.Start().RequireInverseRatioOfAbove(other, bound);
            }

            public Flow<double> EnsureInverseRatioOfAbove(double other, double bound)
            {
                return value.Start().EnsureInverseRatioOfAbove(other, bound);
            }
        }
    }
}

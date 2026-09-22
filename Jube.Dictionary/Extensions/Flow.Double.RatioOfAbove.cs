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
            public Flow<double> MatchRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRatioOfAbove(flow.Value, denominator, bound));
            }

            public Flow<double> RejectRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRatioOfAbove(flow.Value, denominator, bound));
            }

            public Flow<double> BreakRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRatioOfAbove(flow.Value, denominator, bound));
            }

            public Flow<double> RequireRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRatioOfAbove(flow.Value, denominator, bound));
            }

            public Flow<double> EnsureRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRatioOfAbove(flow.Value, denominator, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchRatioOfAbove(double denominator, double bound)
            {
                return value.Start().MatchRatioOfAbove(denominator, bound);
            }

            public Flow<double> RejectRatioOfAbove(double denominator, double bound)
            {
                return value.Start().RejectRatioOfAbove(denominator, bound);
            }

            public Flow<double> BreakRatioOfAbove(double denominator, double bound)
            {
                return value.Start().BreakRatioOfAbove(denominator, bound);
            }

            public Flow<double> RequireRatioOfAbove(double denominator, double bound)
            {
                return value.Start().RequireRatioOfAbove(denominator, bound);
            }

            public Flow<double> EnsureRatioOfAbove(double denominator, double bound)
            {
                return value.Start().EnsureRatioOfAbove(denominator, bound);
            }
        }
    }
}

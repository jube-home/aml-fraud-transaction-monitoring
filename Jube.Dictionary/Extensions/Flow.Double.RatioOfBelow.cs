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
            public Flow<double> MatchRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRatioOfBelow(flow.Value, denominator, bound));
            }

            public Flow<double> RejectRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRatioOfBelow(flow.Value, denominator, bound));
            }

            public Flow<double> BreakRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRatioOfBelow(flow.Value, denominator, bound));
            }

            public Flow<double> RequireRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRatioOfBelow(flow.Value, denominator, bound));
            }

            public Flow<double> EnsureRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRatioOfBelow(flow.Value, denominator, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchRatioOfBelow(double denominator, double bound)
            {
                return value.Start().MatchRatioOfBelow(denominator, bound);
            }

            public Flow<double> RejectRatioOfBelow(double denominator, double bound)
            {
                return value.Start().RejectRatioOfBelow(denominator, bound);
            }

            public Flow<double> BreakRatioOfBelow(double denominator, double bound)
            {
                return value.Start().BreakRatioOfBelow(denominator, bound);
            }

            public Flow<double> RequireRatioOfBelow(double denominator, double bound)
            {
                return value.Start().RequireRatioOfBelow(denominator, bound);
            }

            public Flow<double> EnsureRatioOfBelow(double denominator, double bound)
            {
                return value.Start().EnsureRatioOfBelow(denominator, bound);
            }
        }
    }
}

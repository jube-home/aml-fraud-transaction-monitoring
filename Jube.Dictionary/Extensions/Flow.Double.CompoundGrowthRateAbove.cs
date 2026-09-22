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
            public Flow<double> MatchCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }

            public Flow<double> RejectCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }

            public Flow<double> BreakCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }

            public Flow<double> RequireCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }

            public Flow<double> EnsureCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return value.Start().MatchCompoundGrowthRateAbove(previous, periods, bound);
            }

            public Flow<double> RejectCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return value.Start().RejectCompoundGrowthRateAbove(previous, periods, bound);
            }

            public Flow<double> BreakCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return value.Start().BreakCompoundGrowthRateAbove(previous, periods, bound);
            }

            public Flow<double> RequireCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return value.Start().RequireCompoundGrowthRateAbove(previous, periods, bound);
            }

            public Flow<double> EnsureCompoundGrowthRateAbove(double previous, double periods, double bound)
            {
                return value.Start().EnsureCompoundGrowthRateAbove(previous, periods, bound);
            }
        }
    }
}

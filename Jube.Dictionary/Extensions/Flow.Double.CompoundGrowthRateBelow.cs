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
            public Flow<double> MatchCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleCompoundGrowthRateBelow(flow.Value, previous, periods, bound));
            }

            public Flow<double> RejectCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleCompoundGrowthRateBelow(flow.Value, previous, periods, bound));
            }

            public Flow<double> BreakCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleCompoundGrowthRateBelow(flow.Value, previous, periods, bound));
            }

            public Flow<double> RequireCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleCompoundGrowthRateBelow(flow.Value, previous, periods, bound));
            }

            public Flow<double> EnsureCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleCompoundGrowthRateBelow(flow.Value, previous, periods, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return value.Start().MatchCompoundGrowthRateBelow(previous, periods, bound);
            }

            public Flow<double> RejectCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return value.Start().RejectCompoundGrowthRateBelow(previous, periods, bound);
            }

            public Flow<double> BreakCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return value.Start().BreakCompoundGrowthRateBelow(previous, periods, bound);
            }

            public Flow<double> RequireCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return value.Start().RequireCompoundGrowthRateBelow(previous, periods, bound);
            }

            public Flow<double> EnsureCompoundGrowthRateBelow(double previous, double periods, double bound)
            {
                return value.Start().EnsureCompoundGrowthRateBelow(previous, periods, bound);
            }
        }
    }
}

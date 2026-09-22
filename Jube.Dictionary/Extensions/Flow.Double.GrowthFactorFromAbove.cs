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
            public Flow<double> MatchGrowthFactorFromAbove(double previous, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleGrowthFactorFromAbove(flow.Value, previous, bound));
            }

            public Flow<double> RejectGrowthFactorFromAbove(double previous, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleGrowthFactorFromAbove(flow.Value, previous, bound));
            }

            public Flow<double> BreakGrowthFactorFromAbove(double previous, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleGrowthFactorFromAbove(flow.Value, previous, bound));
            }

            public Flow<double> RequireGrowthFactorFromAbove(double previous, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleGrowthFactorFromAbove(flow.Value, previous, bound));
            }

            public Flow<double> EnsureGrowthFactorFromAbove(double previous, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleGrowthFactorFromAbove(flow.Value, previous, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchGrowthFactorFromAbove(double previous, double bound)
            {
                return value.Start().MatchGrowthFactorFromAbove(previous, bound);
            }

            public Flow<double> RejectGrowthFactorFromAbove(double previous, double bound)
            {
                return value.Start().RejectGrowthFactorFromAbove(previous, bound);
            }

            public Flow<double> BreakGrowthFactorFromAbove(double previous, double bound)
            {
                return value.Start().BreakGrowthFactorFromAbove(previous, bound);
            }

            public Flow<double> RequireGrowthFactorFromAbove(double previous, double bound)
            {
                return value.Start().RequireGrowthFactorFromAbove(previous, bound);
            }

            public Flow<double> EnsureGrowthFactorFromAbove(double previous, double bound)
            {
                return value.Start().EnsureGrowthFactorFromAbove(previous, bound);
            }
        }
    }
}

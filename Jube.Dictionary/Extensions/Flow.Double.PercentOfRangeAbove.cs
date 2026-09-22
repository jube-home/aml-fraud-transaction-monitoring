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
            public Flow<double> MatchPercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }

            public Flow<double> RejectPercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }

            public Flow<double> BreakPercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }

            public Flow<double> RequirePercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }

            public Flow<double> EnsurePercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchPercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return value.Start().MatchPercentOfRangeAbove(minimum, maximum, bound);
            }

            public Flow<double> RejectPercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return value.Start().RejectPercentOfRangeAbove(minimum, maximum, bound);
            }

            public Flow<double> BreakPercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return value.Start().BreakPercentOfRangeAbove(minimum, maximum, bound);
            }

            public Flow<double> RequirePercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return value.Start().RequirePercentOfRangeAbove(minimum, maximum, bound);
            }

            public Flow<double> EnsurePercentOfRangeAbove(double minimum, double maximum, double bound)
            {
                return value.Start().EnsurePercentOfRangeAbove(minimum, maximum, bound);
            }
        }
    }
}

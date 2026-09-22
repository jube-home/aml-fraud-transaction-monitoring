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
            public Flow<double> MatchMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleMinMaxNormaliseAbove(flow.Value, minimum, maximum, bound));
            }

            public Flow<double> RejectMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleMinMaxNormaliseAbove(flow.Value, minimum, maximum, bound));
            }

            public Flow<double> BreakMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleMinMaxNormaliseAbove(flow.Value, minimum, maximum, bound));
            }

            public Flow<double> RequireMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleMinMaxNormaliseAbove(flow.Value, minimum, maximum, bound));
            }

            public Flow<double> EnsureMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleMinMaxNormaliseAbove(flow.Value, minimum, maximum, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return value.Start().MatchMinMaxNormaliseAbove(minimum, maximum, bound);
            }

            public Flow<double> RejectMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return value.Start().RejectMinMaxNormaliseAbove(minimum, maximum, bound);
            }

            public Flow<double> BreakMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return value.Start().BreakMinMaxNormaliseAbove(minimum, maximum, bound);
            }

            public Flow<double> RequireMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return value.Start().RequireMinMaxNormaliseAbove(minimum, maximum, bound);
            }

            public Flow<double> EnsureMinMaxNormaliseAbove(double minimum, double maximum, double bound)
            {
                return value.Start().EnsureMinMaxNormaliseAbove(minimum, maximum, bound);
            }
        }
    }
}

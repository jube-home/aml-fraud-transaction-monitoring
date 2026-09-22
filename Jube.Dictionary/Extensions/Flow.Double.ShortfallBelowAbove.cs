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
            public Flow<double> MatchShortfallBelowAbove(double minimum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleShortfallBelowAbove(flow.Value, minimum, bound));
            }

            public Flow<double> RejectShortfallBelowAbove(double minimum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleShortfallBelowAbove(flow.Value, minimum, bound));
            }

            public Flow<double> BreakShortfallBelowAbove(double minimum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleShortfallBelowAbove(flow.Value, minimum, bound));
            }

            public Flow<double> RequireShortfallBelowAbove(double minimum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleShortfallBelowAbove(flow.Value, minimum, bound));
            }

            public Flow<double> EnsureShortfallBelowAbove(double minimum, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleShortfallBelowAbove(flow.Value, minimum, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchShortfallBelowAbove(double minimum, double bound)
            {
                return value.Start().MatchShortfallBelowAbove(minimum, bound);
            }

            public Flow<double> RejectShortfallBelowAbove(double minimum, double bound)
            {
                return value.Start().RejectShortfallBelowAbove(minimum, bound);
            }

            public Flow<double> BreakShortfallBelowAbove(double minimum, double bound)
            {
                return value.Start().BreakShortfallBelowAbove(minimum, bound);
            }

            public Flow<double> RequireShortfallBelowAbove(double minimum, double bound)
            {
                return value.Start().RequireShortfallBelowAbove(minimum, bound);
            }

            public Flow<double> EnsureShortfallBelowAbove(double minimum, double bound)
            {
                return value.Start().EnsureShortfallBelowAbove(minimum, bound);
            }
        }
    }
}

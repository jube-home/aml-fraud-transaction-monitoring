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
            public Flow<double> MatchLogRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }

            public Flow<double> RejectLogRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }

            public Flow<double> BreakLogRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }

            public Flow<double> RequireLogRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }

            public Flow<double> EnsureLogRatioOfAbove(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchLogRatioOfAbove(double denominator, double bound)
            {
                return value.Start().MatchLogRatioOfAbove(denominator, bound);
            }

            public Flow<double> RejectLogRatioOfAbove(double denominator, double bound)
            {
                return value.Start().RejectLogRatioOfAbove(denominator, bound);
            }

            public Flow<double> BreakLogRatioOfAbove(double denominator, double bound)
            {
                return value.Start().BreakLogRatioOfAbove(denominator, bound);
            }

            public Flow<double> RequireLogRatioOfAbove(double denominator, double bound)
            {
                return value.Start().RequireLogRatioOfAbove(denominator, bound);
            }

            public Flow<double> EnsureLogRatioOfAbove(double denominator, double bound)
            {
                return value.Start().EnsureLogRatioOfAbove(denominator, bound);
            }
        }
    }
}

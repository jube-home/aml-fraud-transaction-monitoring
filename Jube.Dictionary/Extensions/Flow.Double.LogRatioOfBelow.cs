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
            public Flow<double> MatchLogRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleLogRatioOfBelow(flow.Value, denominator, bound));
            }

            public Flow<double> RejectLogRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleLogRatioOfBelow(flow.Value, denominator, bound));
            }

            public Flow<double> BreakLogRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleLogRatioOfBelow(flow.Value, denominator, bound));
            }

            public Flow<double> RequireLogRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleLogRatioOfBelow(flow.Value, denominator, bound));
            }

            public Flow<double> EnsureLogRatioOfBelow(double denominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleLogRatioOfBelow(flow.Value, denominator, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchLogRatioOfBelow(double denominator, double bound)
            {
                return value.Start().MatchLogRatioOfBelow(denominator, bound);
            }

            public Flow<double> RejectLogRatioOfBelow(double denominator, double bound)
            {
                return value.Start().RejectLogRatioOfBelow(denominator, bound);
            }

            public Flow<double> BreakLogRatioOfBelow(double denominator, double bound)
            {
                return value.Start().BreakLogRatioOfBelow(denominator, bound);
            }

            public Flow<double> RequireLogRatioOfBelow(double denominator, double bound)
            {
                return value.Start().RequireLogRatioOfBelow(denominator, bound);
            }

            public Flow<double> EnsureLogRatioOfBelow(double denominator, double bound)
            {
                return value.Start().EnsureLogRatioOfBelow(denominator, bound);
            }
        }
    }
}

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
            public Flow<double> MatchWithinPercentOf(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }

            public Flow<double> RejectWithinPercentOf(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }

            public Flow<double> BreakWithinPercentOf(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }

            public Flow<double> RequireWithinPercentOf(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }

            public Flow<double> EnsureWithinPercentOf(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchWithinPercentOf(double target, double percent)
            {
                return value.Start().MatchWithinPercentOf(target, percent);
            }

            public Flow<double> RejectWithinPercentOf(double target, double percent)
            {
                return value.Start().RejectWithinPercentOf(target, percent);
            }

            public Flow<double> BreakWithinPercentOf(double target, double percent)
            {
                return value.Start().BreakWithinPercentOf(target, percent);
            }

            public Flow<double> RequireWithinPercentOf(double target, double percent)
            {
                return value.Start().RequireWithinPercentOf(target, percent);
            }

            public Flow<double> EnsureWithinPercentOf(double target, double percent)
            {
                return value.Start().EnsureWithinPercentOf(target, percent);
            }
        }
    }
}

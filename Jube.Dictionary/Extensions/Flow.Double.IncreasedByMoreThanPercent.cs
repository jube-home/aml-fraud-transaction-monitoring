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
            public Flow<double> MatchIncreasedByMoreThanPercent(double previous, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }

            public Flow<double> RejectIncreasedByMoreThanPercent(double previous, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }

            public Flow<double> BreakIncreasedByMoreThanPercent(double previous, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }

            public Flow<double> RequireIncreasedByMoreThanPercent(double previous, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }

            public Flow<double> EnsureIncreasedByMoreThanPercent(double previous, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIncreasedByMoreThanPercent(double previous, double percent)
            {
                return value.Start().MatchIncreasedByMoreThanPercent(previous, percent);
            }

            public Flow<double> RejectIncreasedByMoreThanPercent(double previous, double percent)
            {
                return value.Start().RejectIncreasedByMoreThanPercent(previous, percent);
            }

            public Flow<double> BreakIncreasedByMoreThanPercent(double previous, double percent)
            {
                return value.Start().BreakIncreasedByMoreThanPercent(previous, percent);
            }

            public Flow<double> RequireIncreasedByMoreThanPercent(double previous, double percent)
            {
                return value.Start().RequireIncreasedByMoreThanPercent(previous, percent);
            }

            public Flow<double> EnsureIncreasedByMoreThanPercent(double previous, double percent)
            {
                return value.Start().EnsureIncreasedByMoreThanPercent(previous, percent);
            }
        }
    }
}

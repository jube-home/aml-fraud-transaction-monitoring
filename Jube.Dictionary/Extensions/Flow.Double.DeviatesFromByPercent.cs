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
            public Flow<double> MatchDeviatesFromByPercent(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }

            public Flow<double> RejectDeviatesFromByPercent(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }

            public Flow<double> BreakDeviatesFromByPercent(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }

            public Flow<double> RequireDeviatesFromByPercent(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }

            public Flow<double> EnsureDeviatesFromByPercent(double target, double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchDeviatesFromByPercent(double target, double percent)
            {
                return value.Start().MatchDeviatesFromByPercent(target, percent);
            }

            public Flow<double> RejectDeviatesFromByPercent(double target, double percent)
            {
                return value.Start().RejectDeviatesFromByPercent(target, percent);
            }

            public Flow<double> BreakDeviatesFromByPercent(double target, double percent)
            {
                return value.Start().BreakDeviatesFromByPercent(target, percent);
            }

            public Flow<double> RequireDeviatesFromByPercent(double target, double percent)
            {
                return value.Start().RequireDeviatesFromByPercent(target, percent);
            }

            public Flow<double> EnsureDeviatesFromByPercent(double target, double percent)
            {
                return value.Start().EnsureDeviatesFromByPercent(target, percent);
            }
        }
    }
}

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
            public Flow<double> MatchComplementInRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleComplementInRange(flow.Value, lowerBound, upperBound));
            }

            public Flow<double> RejectComplementInRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleComplementInRange(flow.Value, lowerBound, upperBound));
            }

            public Flow<double> BreakComplementInRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleComplementInRange(flow.Value, lowerBound, upperBound));
            }

            public Flow<double> RequireComplementInRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleComplementInRange(flow.Value, lowerBound, upperBound));
            }

            public Flow<double> EnsureComplementInRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleComplementInRange(flow.Value, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchComplementInRange(double lowerBound, double upperBound)
            {
                return value.Start().MatchComplementInRange(lowerBound, upperBound);
            }

            public Flow<double> RejectComplementInRange(double lowerBound, double upperBound)
            {
                return value.Start().RejectComplementInRange(lowerBound, upperBound);
            }

            public Flow<double> BreakComplementInRange(double lowerBound, double upperBound)
            {
                return value.Start().BreakComplementInRange(lowerBound, upperBound);
            }

            public Flow<double> RequireComplementInRange(double lowerBound, double upperBound)
            {
                return value.Start().RequireComplementInRange(lowerBound, upperBound);
            }

            public Flow<double> EnsureComplementInRange(double lowerBound, double upperBound)
            {
                return value.Start().EnsureComplementInRange(lowerBound, upperBound);
            }
        }
    }
}

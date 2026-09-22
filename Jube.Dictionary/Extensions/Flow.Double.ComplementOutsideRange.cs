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
            public Flow<double> MatchComplementOutsideRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleComplementOutsideRange(flow.Value, lowerBound, upperBound));
            }

            public Flow<double> RejectComplementOutsideRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleComplementOutsideRange(flow.Value, lowerBound, upperBound));
            }

            public Flow<double> BreakComplementOutsideRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleComplementOutsideRange(flow.Value, lowerBound, upperBound));
            }

            public Flow<double> RequireComplementOutsideRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleComplementOutsideRange(flow.Value, lowerBound, upperBound));
            }

            public Flow<double> EnsureComplementOutsideRange(double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleComplementOutsideRange(flow.Value, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchComplementOutsideRange(double lowerBound, double upperBound)
            {
                return value.Start().MatchComplementOutsideRange(lowerBound, upperBound);
            }

            public Flow<double> RejectComplementOutsideRange(double lowerBound, double upperBound)
            {
                return value.Start().RejectComplementOutsideRange(lowerBound, upperBound);
            }

            public Flow<double> BreakComplementOutsideRange(double lowerBound, double upperBound)
            {
                return value.Start().BreakComplementOutsideRange(lowerBound, upperBound);
            }

            public Flow<double> RequireComplementOutsideRange(double lowerBound, double upperBound)
            {
                return value.Start().RequireComplementOutsideRange(lowerBound, upperBound);
            }

            public Flow<double> EnsureComplementOutsideRange(double lowerBound, double upperBound)
            {
                return value.Start().EnsureComplementOutsideRange(lowerBound, upperBound);
            }
        }
    }
}

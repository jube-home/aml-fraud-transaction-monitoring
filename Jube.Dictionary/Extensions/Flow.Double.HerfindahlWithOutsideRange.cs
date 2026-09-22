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
            public Flow<double> MatchHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> RejectHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> BreakHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> RequireHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> EnsureHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().MatchHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }

            public Flow<double> RejectHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().RejectHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }

            public Flow<double> BreakHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().BreakHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }

            public Flow<double> RequireHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().RequireHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }

            public Flow<double> EnsureHerfindahlWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().EnsureHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }
        }
    }
}

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
            public Flow<double> MatchImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleImbalanceWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RejectImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleImbalanceWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> BreakImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleImbalanceWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RequireImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleImbalanceWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> EnsureImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleImbalanceWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().MatchImbalanceWithOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> RejectImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RejectImbalanceWithOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> BreakImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().BreakImbalanceWithOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> RequireImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RequireImbalanceWithOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> EnsureImbalanceWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().EnsureImbalanceWithOutsideRange(other, lowerBound, upperBound);
            }
        }
    }
}

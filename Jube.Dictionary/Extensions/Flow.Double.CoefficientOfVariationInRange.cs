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
            public Flow<double> MatchCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleCoefficientOfVariationInRange(flow.Value, mean, lowerBound, upperBound));
            }

            public Flow<double> RejectCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleCoefficientOfVariationInRange(flow.Value, mean, lowerBound, upperBound));
            }

            public Flow<double> BreakCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleCoefficientOfVariationInRange(flow.Value, mean, lowerBound, upperBound));
            }

            public Flow<double> RequireCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleCoefficientOfVariationInRange(flow.Value, mean, lowerBound, upperBound));
            }

            public Flow<double> EnsureCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleCoefficientOfVariationInRange(flow.Value, mean, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return value.Start().MatchCoefficientOfVariationInRange(mean, lowerBound, upperBound);
            }

            public Flow<double> RejectCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return value.Start().RejectCoefficientOfVariationInRange(mean, lowerBound, upperBound);
            }

            public Flow<double> BreakCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return value.Start().BreakCoefficientOfVariationInRange(mean, lowerBound, upperBound);
            }

            public Flow<double> RequireCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return value.Start().RequireCoefficientOfVariationInRange(mean, lowerBound, upperBound);
            }

            public Flow<double> EnsureCoefficientOfVariationInRange(double mean, double lowerBound, double upperBound)
            {
                return value.Start().EnsureCoefficientOfVariationInRange(mean, lowerBound, upperBound);
            }
        }
    }
}

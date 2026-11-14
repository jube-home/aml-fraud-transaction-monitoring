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
            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRatioOfRatiosInRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }

            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRatioOfRatiosInRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }

            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRatioOfRatiosInRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }

            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRatioOfRatiosInRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }

            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRatioOfRatiosInRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRatioOfRatiosInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureRatioOfRatiosInRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }
        }
    }
}

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
            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }

            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }

            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }

            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }

            [Description("True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value over its denominator, divided by another numerator over its own denominator is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRatioOfRatiosBelow([Description("The number the value is divided by.")] double denominator,
                [Description("The numerator of the other ratio.")] double otherNumerator,
                [Description("The denominator of the other ratio.")] double otherDenominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }
        }
    }
}

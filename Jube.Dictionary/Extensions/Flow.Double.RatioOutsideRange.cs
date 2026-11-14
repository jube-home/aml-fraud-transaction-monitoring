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
            [Description("True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRatioOutsideRange(flow.Value, denominator, minimum, maximum));
            }

            [Description("True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRatioOutsideRange(flow.Value, denominator, minimum, maximum));
            }

            [Description("True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRatioOutsideRange(flow.Value, denominator, minimum, maximum));
            }

            [Description("True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRatioOutsideRange(flow.Value, denominator, minimum, maximum));
            }

            [Description("True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRatioOutsideRange(flow.Value, denominator, minimum, maximum));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().MatchRatioOutsideRange(denominator, minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().RejectRatioOutsideRange(denominator, minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().BreakRatioOutsideRange(denominator, minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().RequireRatioOutsideRange(denominator, minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: the ratio is below the minimum or above the maximum. A zero denominator is false. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRatioOutsideRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().EnsureRatioOutsideRange(denominator, minimum, maximum);
            }
        }
    }
}

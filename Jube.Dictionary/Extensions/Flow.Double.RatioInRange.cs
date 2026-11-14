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
            [Description("True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRatioInRange(flow.Value, denominator, minimum, maximum));
            }

            [Description("True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRatioInRange(flow.Value, denominator, minimum, maximum));
            }

            [Description("True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRatioInRange(flow.Value, denominator, minimum, maximum));
            }

            [Description("True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRatioInRange(flow.Value, denominator, minimum, maximum));
            }

            [Description("True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRatioInRange(flow.Value, denominator, minimum, maximum));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().MatchRatioInRange(denominator, minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().RejectRatioInRange(denominator, minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().BreakRatioInRange(denominator, minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().RequireRatioInRange(denominator, minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: the value divided by the denominator is between the bounds, both included. A zero denominator is false. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRatioInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound.")] double minimum,
                [Description("The upper bound.")] double maximum)
            {
                return value.Start().EnsureRatioInRange(denominator, minimum, maximum);
            }
        }
    }
}

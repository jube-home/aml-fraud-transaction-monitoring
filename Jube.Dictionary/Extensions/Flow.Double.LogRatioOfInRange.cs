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
            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }

            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }

            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }

            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }

            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchLogRatioOfInRange(denominator, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectLogRatioOfInRange(denominator, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakLogRatioOfInRange(denominator, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireLogRatioOfInRange(denominator, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureLogRatioOfInRange([Description("The number the value is divided by.")] double denominator,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureLogRatioOfInRange(denominator, lowerBound, upperBound);
            }
        }
    }
}

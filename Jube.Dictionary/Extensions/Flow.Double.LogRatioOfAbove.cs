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
            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }

            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }

            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }

            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }

            [Description("True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleLogRatioOfAbove(flow.Value, denominator, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchLogRatioOfAbove(denominator, bound);
            }

            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectLogRatioOfAbove(denominator, bound);
            }

            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakLogRatioOfAbove(denominator, bound);
            }

            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireLogRatioOfAbove(denominator, bound);
            }

            [Description("Begins a pipeline from the value. True when: the natural log of the value over the denominator, symmetric for increases and decreases is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureLogRatioOfAbove([Description("The number the value is divided by.")] double denominator,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureLogRatioOfAbove(denominator, bound);
            }
        }
    }
}

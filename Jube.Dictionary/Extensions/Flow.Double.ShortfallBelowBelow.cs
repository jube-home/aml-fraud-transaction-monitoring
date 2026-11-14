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
            [Description("True when: how far the value is under the minimum, zero when at or above it is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleShortfallBelowBelow(flow.Value, minimum, bound));
            }

            [Description("True when: how far the value is under the minimum, zero when at or above it is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleShortfallBelowBelow(flow.Value, minimum, bound));
            }

            [Description("True when: how far the value is under the minimum, zero when at or above it is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleShortfallBelowBelow(flow.Value, minimum, bound));
            }

            [Description("True when: how far the value is under the minimum, zero when at or above it is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleShortfallBelowBelow(flow.Value, minimum, bound));
            }

            [Description("True when: how far the value is under the minimum, zero when at or above it is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleShortfallBelowBelow(flow.Value, minimum, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchShortfallBelowBelow(minimum, bound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectShortfallBelowBelow(minimum, bound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakShortfallBelowBelow(minimum, bound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireShortfallBelowBelow(minimum, bound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureShortfallBelowBelow([Description("The lower bound.")] double minimum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureShortfallBelowBelow(minimum, bound);
            }
        }
    }
}

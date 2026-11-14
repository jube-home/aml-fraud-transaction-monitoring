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
            [Description("True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchPercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }

            [Description("True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectPercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }

            [Description("True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakPercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }

            [Description("True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequirePercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }

            [Description("True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsurePercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentOfRangeAbove(flow.Value, minimum, maximum, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchPercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchPercentOfRangeAbove(minimum, maximum, bound);
            }

            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectPercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectPercentOfRangeAbove(minimum, maximum, bound);
            }

            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakPercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakPercentOfRangeAbove(minimum, maximum, bound);
            }

            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequirePercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequirePercentOfRangeAbove(minimum, maximum, bound);
            }

            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (100) is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsurePercentOfRangeAbove([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 100.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsurePercentOfRangeAbove(minimum, maximum, bound);
            }
        }
    }
}

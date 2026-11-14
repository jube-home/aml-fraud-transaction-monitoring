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
            [Description("True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleMinMaxNormaliseBelow(flow.Value, minimum, maximum, bound));
            }

            [Description("True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleMinMaxNormaliseBelow(flow.Value, minimum, maximum, bound));
            }

            [Description("True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleMinMaxNormaliseBelow(flow.Value, minimum, maximum, bound));
            }

            [Description("True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleMinMaxNormaliseBelow(flow.Value, minimum, maximum, bound));
            }

            [Description("True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleMinMaxNormaliseBelow(flow.Value, minimum, maximum, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchMinMaxNormaliseBelow(minimum, maximum, bound);
            }

            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectMinMaxNormaliseBelow(minimum, maximum, bound);
            }

            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakMinMaxNormaliseBelow(minimum, maximum, bound);
            }

            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireMinMaxNormaliseBelow(minimum, maximum, bound);
            }

            [Description("Begins a pipeline from the value. True when: where the value sits between the minimum (0) and maximum (1) is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureMinMaxNormaliseBelow([Description("The value that maps to 0.")] double minimum,
                [Description("The value that maps to 1.")] double maximum,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureMinMaxNormaliseBelow(minimum, maximum, bound);
            }
        }
    }
}

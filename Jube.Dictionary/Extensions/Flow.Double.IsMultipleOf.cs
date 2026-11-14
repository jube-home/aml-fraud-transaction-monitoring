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
            [Description("True when: the value is an exact multiple of the divisor. A zero divisor is false. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }

            [Description("True when: the value is an exact multiple of the divisor. A zero divisor is false. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }

            [Description("True when: the value is an exact multiple of the divisor. A zero divisor is false. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }

            [Description("True when: the value is an exact multiple of the divisor. A zero divisor is false. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }

            [Description("True when: the value is an exact multiple of the divisor. A zero divisor is false. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsMultipleOf(flow.Value, divisor));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is an exact multiple of the divisor. A zero divisor is false. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return value.Start().MatchIsMultipleOf(divisor);
            }

            [Description("Begins a pipeline from the value. True when: the value is an exact multiple of the divisor. A zero divisor is false. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return value.Start().RejectIsMultipleOf(divisor);
            }

            [Description("Begins a pipeline from the value. True when: the value is an exact multiple of the divisor. A zero divisor is false. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return value.Start().BreakIsMultipleOf(divisor);
            }

            [Description("Begins a pipeline from the value. True when: the value is an exact multiple of the divisor. A zero divisor is false. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return value.Start().RequireIsMultipleOf(divisor);
            }

            [Description("Begins a pipeline from the value. True when: the value is an exact multiple of the divisor. A zero divisor is false. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsMultipleOf([Description("The number the value must divide by exactly.")] double divisor)
            {
                return value.Start().EnsureIsMultipleOf(divisor);
            }
        }
    }
}

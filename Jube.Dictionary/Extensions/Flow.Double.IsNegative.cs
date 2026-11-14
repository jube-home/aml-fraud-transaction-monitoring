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
            [Description("True when: the value is less than zero. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsNegative(flow.Value));
            }

            [Description("True when: the value is less than zero. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsNegative(flow.Value));
            }

            [Description("True when: the value is less than zero. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsNegative(flow.Value));
            }

            [Description("True when: the value is less than zero. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsNegative(flow.Value));
            }

            [Description("True when: the value is less than zero. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsNegative()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsNegative(flow.Value));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is less than zero. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsNegative()
            {
                return value.Start().MatchIsNegative();
            }

            [Description("Begins a pipeline from the value. True when: the value is less than zero. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsNegative()
            {
                return value.Start().RejectIsNegative();
            }

            [Description("Begins a pipeline from the value. True when: the value is less than zero. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsNegative()
            {
                return value.Start().BreakIsNegative();
            }

            [Description("Begins a pipeline from the value. True when: the value is less than zero. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsNegative()
            {
                return value.Start().RequireIsNegative();
            }

            [Description("Begins a pipeline from the value. True when: the value is less than zero. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsNegative()
            {
                return value.Start().EnsureIsNegative();
            }
        }
    }
}

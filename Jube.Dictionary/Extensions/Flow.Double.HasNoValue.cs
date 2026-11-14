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
            [Description("True when: the value is NaN, for example an undefined calculation. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchHasNoValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHasNoValue(flow.Value));
            }

            [Description("True when: the value is NaN, for example an undefined calculation. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectHasNoValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHasNoValue(flow.Value));
            }

            [Description("True when: the value is NaN, for example an undefined calculation. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakHasNoValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHasNoValue(flow.Value));
            }

            [Description("True when: the value is NaN, for example an undefined calculation. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireHasNoValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHasNoValue(flow.Value));
            }

            [Description("True when: the value is NaN, for example an undefined calculation. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureHasNoValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHasNoValue(flow.Value));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is NaN, for example an undefined calculation. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchHasNoValue()
            {
                return value.Start().MatchHasNoValue();
            }

            [Description("Begins a pipeline from the value. True when: the value is NaN, for example an undefined calculation. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectHasNoValue()
            {
                return value.Start().RejectHasNoValue();
            }

            [Description("Begins a pipeline from the value. True when: the value is NaN, for example an undefined calculation. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakHasNoValue()
            {
                return value.Start().BreakHasNoValue();
            }

            [Description("Begins a pipeline from the value. True when: the value is NaN, for example an undefined calculation. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireHasNoValue()
            {
                return value.Start().RequireHasNoValue();
            }

            [Description("Begins a pipeline from the value. True when: the value is NaN, for example an undefined calculation. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureHasNoValue()
            {
                return value.Start().EnsureHasNoValue();
            }
        }
    }
}

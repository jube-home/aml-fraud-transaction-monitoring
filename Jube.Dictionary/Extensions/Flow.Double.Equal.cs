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
            [Description("True when: the value equals the argument. NaN never equals anything. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleEqual(flow.Value, other));
            }

            [Description("True when: the value equals the argument. NaN never equals anything. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleEqual(flow.Value, other));
            }

            [Description("True when: the value equals the argument. NaN never equals anything. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleEqual(flow.Value, other));
            }

            [Description("True when: the value equals the argument. NaN never equals anything. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleEqual(flow.Value, other));
            }

            [Description("True when: the value equals the argument. NaN never equals anything. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleEqual(flow.Value, other));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value equals the argument. NaN never equals anything. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchEqual([Description("The other number.")] double other)
            {
                return value.Start().MatchEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: the value equals the argument. NaN never equals anything. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectEqual([Description("The other number.")] double other)
            {
                return value.Start().RejectEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: the value equals the argument. NaN never equals anything. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakEqual([Description("The other number.")] double other)
            {
                return value.Start().BreakEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: the value equals the argument. NaN never equals anything. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireEqual([Description("The other number.")] double other)
            {
                return value.Start().RequireEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: the value equals the argument. NaN never equals anything. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureEqual([Description("The other number.")] double other)
            {
                return value.Start().EnsureEqual(other);
            }
        }
    }
}

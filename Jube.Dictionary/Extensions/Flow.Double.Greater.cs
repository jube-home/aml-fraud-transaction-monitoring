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
            [Description("True when: the value is greater than the argument. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchGreater([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleGreater(flow.Value, other));
            }

            [Description("True when: the value is greater than the argument. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectGreater([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleGreater(flow.Value, other));
            }

            [Description("True when: the value is greater than the argument. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakGreater([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleGreater(flow.Value, other));
            }

            [Description("True when: the value is greater than the argument. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireGreater([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleGreater(flow.Value, other));
            }

            [Description("True when: the value is greater than the argument. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureGreater([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleGreater(flow.Value, other));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is greater than the argument. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchGreater([Description("The other number.")] double other)
            {
                return value.Start().MatchGreater(other);
            }

            [Description("Begins a pipeline from the value. True when: the value is greater than the argument. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectGreater([Description("The other number.")] double other)
            {
                return value.Start().RejectGreater(other);
            }

            [Description("Begins a pipeline from the value. True when: the value is greater than the argument. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakGreater([Description("The other number.")] double other)
            {
                return value.Start().BreakGreater(other);
            }

            [Description("Begins a pipeline from the value. True when: the value is greater than the argument. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireGreater([Description("The other number.")] double other)
            {
                return value.Start().RequireGreater(other);
            }

            [Description("Begins a pipeline from the value. True when: the value is greater than the argument. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureGreater([Description("The other number.")] double other)
            {
                return value.Start().EnsureGreater(other);
            }
        }
    }
}

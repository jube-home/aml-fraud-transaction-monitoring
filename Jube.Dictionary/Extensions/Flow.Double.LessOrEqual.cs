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
            [Description("True when: the value is less than or equal to the argument. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchLessOrEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleLessOrEqual(flow.Value, other));
            }

            [Description("True when: the value is less than or equal to the argument. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectLessOrEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleLessOrEqual(flow.Value, other));
            }

            [Description("True when: the value is less than or equal to the argument. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakLessOrEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleLessOrEqual(flow.Value, other));
            }

            [Description("True when: the value is less than or equal to the argument. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireLessOrEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleLessOrEqual(flow.Value, other));
            }

            [Description("True when: the value is less than or equal to the argument. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureLessOrEqual([Description("The other number.")] double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleLessOrEqual(flow.Value, other));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is less than or equal to the argument. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchLessOrEqual([Description("The other number.")] double other)
            {
                return value.Start().MatchLessOrEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: the value is less than or equal to the argument. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectLessOrEqual([Description("The other number.")] double other)
            {
                return value.Start().RejectLessOrEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: the value is less than or equal to the argument. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakLessOrEqual([Description("The other number.")] double other)
            {
                return value.Start().BreakLessOrEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: the value is less than or equal to the argument. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireLessOrEqual([Description("The other number.")] double other)
            {
                return value.Start().RequireLessOrEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: the value is less than or equal to the argument. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureLessOrEqual([Description("The other number.")] double other)
            {
                return value.Start().EnsureLessOrEqual(other);
            }
        }
    }
}

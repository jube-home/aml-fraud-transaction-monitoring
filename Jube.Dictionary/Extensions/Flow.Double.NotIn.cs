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
            [Description("True when: the value is none of the listed numbers. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleNotIn(flow.Value, values));
            }

            [Description("True when: the value is none of the listed numbers. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleNotIn(flow.Value, values));
            }

            [Description("True when: the value is none of the listed numbers. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleNotIn(flow.Value, values));
            }

            [Description("True when: the value is none of the listed numbers. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleNotIn(flow.Value, values));
            }

            [Description("True when: the value is none of the listed numbers. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleNotIn(flow.Value, values));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is none of the listed numbers. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return value.Start().MatchNotIn(values);
            }

            [Description("Begins a pipeline from the value. True when: the value is none of the listed numbers. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return value.Start().RejectNotIn(values);
            }

            [Description("Begins a pipeline from the value. True when: the value is none of the listed numbers. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return value.Start().BreakNotIn(values);
            }

            [Description("Begins a pipeline from the value. True when: the value is none of the listed numbers. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return value.Start().RequireNotIn(values);
            }

            [Description("Begins a pipeline from the value. True when: the value is none of the listed numbers. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureNotIn([Description("The value list to compare with the object.")] params double[] values)
            {
                return value.Start().EnsureNotIn(values);
            }
        }
    }
}

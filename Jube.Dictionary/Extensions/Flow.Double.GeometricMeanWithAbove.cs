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
            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }

            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }

            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }

            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }

            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchGeometricMeanWithAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectGeometricMeanWithAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakGeometricMeanWithAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireGeometricMeanWithAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureGeometricMeanWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureGeometricMeanWithAbove(other, bound);
            }
        }
    }
}

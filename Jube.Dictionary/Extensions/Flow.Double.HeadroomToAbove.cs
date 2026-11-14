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
            [Description("True when: the limit minus the value, the room left is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }

            [Description("True when: the limit minus the value, the room left is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }

            [Description("True when: the limit minus the value, the room left is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }

            [Description("True when: the limit minus the value, the room left is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }

            [Description("True when: the limit minus the value, the room left is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the limit minus the value, the room left is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchHeadroomToAbove(limit, bound);
            }

            [Description("Begins a pipeline from the value. True when: the limit minus the value, the room left is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectHeadroomToAbove(limit, bound);
            }

            [Description("Begins a pipeline from the value. True when: the limit minus the value, the room left is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakHeadroomToAbove(limit, bound);
            }

            [Description("Begins a pipeline from the value. True when: the limit minus the value, the room left is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireHeadroomToAbove(limit, bound);
            }

            [Description("Begins a pipeline from the value. True when: the limit minus the value, the room left is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureHeadroomToAbove([Description("The limit.")] double limit,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureHeadroomToAbove(limit, bound);
            }
        }
    }
}

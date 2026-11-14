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
            [Description("True when: the change from the previous value per unit of time is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }

            [Description("True when: the change from the previous value per unit of time is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }

            [Description("True when: the change from the previous value per unit of time is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }

            [Description("True when: the change from the previous value per unit of time is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }

            [Description("True when: the change from the previous value per unit of time is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the change from the previous value per unit of time is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchSlopeFromAbove(previous, elapsed, bound);
            }

            [Description("Begins a pipeline from the value. True when: the change from the previous value per unit of time is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectSlopeFromAbove(previous, elapsed, bound);
            }

            [Description("Begins a pipeline from the value. True when: the change from the previous value per unit of time is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakSlopeFromAbove(previous, elapsed, bound);
            }

            [Description("Begins a pipeline from the value. True when: the change from the previous value per unit of time is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireSlopeFromAbove(previous, elapsed, bound);
            }

            [Description("Begins a pipeline from the value. True when: the change from the previous value per unit of time is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureSlopeFromAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The time between the two values, in any unit; the result is per that unit.")] double elapsed,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureSlopeFromAbove(previous, elapsed, bound);
            }
        }
    }
}

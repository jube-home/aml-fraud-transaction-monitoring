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
            [Description("True when: the change from the previous value as a fraction of it is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRelativeChangeFromInRange(flow.Value, previous, lowerBound, upperBound));
            }

            [Description("True when: the change from the previous value as a fraction of it is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRelativeChangeFromInRange(flow.Value, previous, lowerBound, upperBound));
            }

            [Description("True when: the change from the previous value as a fraction of it is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRelativeChangeFromInRange(flow.Value, previous, lowerBound, upperBound));
            }

            [Description("True when: the change from the previous value as a fraction of it is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRelativeChangeFromInRange(flow.Value, previous, lowerBound, upperBound));
            }

            [Description("True when: the change from the previous value as a fraction of it is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRelativeChangeFromInRange(flow.Value, previous, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the change from the previous value as a fraction of it is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchRelativeChangeFromInRange(previous, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the change from the previous value as a fraction of it is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectRelativeChangeFromInRange(previous, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the change from the previous value as a fraction of it is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakRelativeChangeFromInRange(previous, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the change from the previous value as a fraction of it is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireRelativeChangeFromInRange(previous, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the change from the previous value as a fraction of it is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRelativeChangeFromInRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureRelativeChangeFromInRange(previous, lowerBound, upperBound);
            }
        }
    }
}

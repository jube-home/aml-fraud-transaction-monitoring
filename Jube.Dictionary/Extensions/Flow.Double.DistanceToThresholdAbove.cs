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
            [Description("True when: the gap between the value and the threshold in either direction is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDistanceToThresholdAbove(flow.Value, threshold, bound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDistanceToThresholdAbove(flow.Value, threshold, bound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDistanceToThresholdAbove(flow.Value, threshold, bound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDistanceToThresholdAbove(flow.Value, threshold, bound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDistanceToThresholdAbove(flow.Value, threshold, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchDistanceToThresholdAbove(threshold, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectDistanceToThresholdAbove(threshold, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakDistanceToThresholdAbove(threshold, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireDistanceToThresholdAbove(threshold, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceToThresholdAbove([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureDistanceToThresholdAbove(threshold, bound);
            }
        }
    }
}

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
            [Description("True when: the gap between the value and the threshold in either direction is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchDistanceToThresholdBelow(threshold, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectDistanceToThresholdBelow(threshold, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakDistanceToThresholdBelow(threshold, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireDistanceToThresholdBelow(threshold, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceToThresholdBelow([Description("The threshold the value is tested against.")] double threshold,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureDistanceToThresholdBelow(threshold, bound);
            }
        }
    }
}

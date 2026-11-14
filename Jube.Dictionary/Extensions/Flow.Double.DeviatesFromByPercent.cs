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
            [Description("True when: the value is further than the percentage from the target. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }

            [Description("True when: the value is further than the percentage from the target. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }

            [Description("True when: the value is further than the percentage from the target. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }

            [Description("True when: the value is further than the percentage from the target. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }

            [Description("True when: the value is further than the percentage from the target. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDeviatesFromByPercent(flow.Value, target, percent));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is further than the percentage from the target. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return value.Start().MatchDeviatesFromByPercent(target, percent);
            }

            [Description("Begins a pipeline from the value. True when: the value is further than the percentage from the target. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return value.Start().RejectDeviatesFromByPercent(target, percent);
            }

            [Description("Begins a pipeline from the value. True when: the value is further than the percentage from the target. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return value.Start().BreakDeviatesFromByPercent(target, percent);
            }

            [Description("Begins a pipeline from the value. True when: the value is further than the percentage from the target. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return value.Start().RequireDeviatesFromByPercent(target, percent);
            }

            [Description("Begins a pipeline from the value. True when: the value is further than the percentage from the target. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDeviatesFromByPercent([Description("The value the deviation is measured from.")] double target,
                [Description("The percentage deviation that counts.")] double percent)
            {
                return value.Start().EnsureDeviatesFromByPercent(target, percent);
            }
        }
    }
}

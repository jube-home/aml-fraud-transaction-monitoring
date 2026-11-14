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
            [Description("True when: the value is within the percentage of the target, in either direction. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }

            [Description("True when: the value is within the percentage of the target, in either direction. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }

            [Description("True when: the value is within the percentage of the target, in either direction. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }

            [Description("True when: the value is within the percentage of the target, in either direction. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }

            [Description("True when: the value is within the percentage of the target, in either direction. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleWithinPercentOf(flow.Value, target, percent));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is within the percentage of the target, in either direction. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return value.Start().MatchWithinPercentOf(target, percent);
            }

            [Description("Begins a pipeline from the value. True when: the value is within the percentage of the target, in either direction. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return value.Start().RejectWithinPercentOf(target, percent);
            }

            [Description("Begins a pipeline from the value. True when: the value is within the percentage of the target, in either direction. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return value.Start().BreakWithinPercentOf(target, percent);
            }

            [Description("Begins a pipeline from the value. True when: the value is within the percentage of the target, in either direction. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return value.Start().RequireWithinPercentOf(target, percent);
            }

            [Description("Begins a pipeline from the value. True when: the value is within the percentage of the target, in either direction. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureWithinPercentOf([Description("The value to compare against.")] double target,
                [Description("The allowed tolerance, as a percentage of target.")] double percent)
            {
                return value.Start().EnsureWithinPercentOf(target, percent);
            }
        }
    }
}

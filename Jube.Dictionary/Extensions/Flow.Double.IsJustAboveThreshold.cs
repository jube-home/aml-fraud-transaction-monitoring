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
            [Description("True when: above the threshold but within the margin percentage of it. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsJustAboveThreshold(flow.Value, threshold, marginPercent));
            }

            [Description("True when: above the threshold but within the margin percentage of it. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsJustAboveThreshold(flow.Value, threshold, marginPercent));
            }

            [Description("True when: above the threshold but within the margin percentage of it. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsJustAboveThreshold(flow.Value, threshold, marginPercent));
            }

            [Description("True when: above the threshold but within the margin percentage of it. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsJustAboveThreshold(flow.Value, threshold, marginPercent));
            }

            [Description("True when: above the threshold but within the margin percentage of it. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsJustAboveThreshold(flow.Value, threshold, marginPercent));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: above the threshold but within the margin percentage of it. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return value.Start().MatchIsJustAboveThreshold(threshold, marginPercent);
            }

            [Description("Begins a pipeline from the value. True when: above the threshold but within the margin percentage of it. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return value.Start().RejectIsJustAboveThreshold(threshold, marginPercent);
            }

            [Description("Begins a pipeline from the value. True when: above the threshold but within the margin percentage of it. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return value.Start().BreakIsJustAboveThreshold(threshold, marginPercent);
            }

            [Description("Begins a pipeline from the value. True when: above the threshold but within the margin percentage of it. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return value.Start().RequireIsJustAboveThreshold(threshold, marginPercent);
            }

            [Description("Begins a pipeline from the value. True when: above the threshold but within the margin percentage of it. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsJustAboveThreshold([Description("The threshold the value is tested against.")] double threshold,
                [Description("How far above the threshold still counts, as a percentage of it.")] double marginPercent)
            {
                return value.Start().EnsureIsJustAboveThreshold(threshold, marginPercent);
            }
        }
    }
}

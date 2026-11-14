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
            [Description("True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            [Description("True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            [Description("True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            [Description("True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }

            [Description("True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsJustBelowAnyThreshold(flow.Value, marginPercent, thresholds));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return value.Start().MatchIsJustBelowAnyThreshold(marginPercent, thresholds);
            }

            [Description("Begins a pipeline from the value. True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return value.Start().RejectIsJustBelowAnyThreshold(marginPercent, thresholds);
            }

            [Description("Begins a pipeline from the value. True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return value.Start().BreakIsJustBelowAnyThreshold(marginPercent, thresholds);
            }

            [Description("Begins a pipeline from the value. True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return value.Start().RequireIsJustBelowAnyThreshold(marginPercent, thresholds);
            }

            [Description("Begins a pipeline from the value. True when: just below any of several limits, within the margin percentage (for example 3,000, 10,000 and 15,000). Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsJustBelowAnyThreshold([Description("How far below a limit still counts, as a percentage of it.")] double marginPercent,
                [Description("The limits to test against, for example 3000, 10000 and 15000.")] params double[] thresholds)
            {
                return value.Start().EnsureIsJustBelowAnyThreshold(marginPercent, thresholds);
            }
        }
    }
}

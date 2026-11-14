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
            [Description("True when: below the threshold but within the margin percentage of it (structuring). Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }

            [Description("True when: below the threshold but within the margin percentage of it (structuring). Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }

            [Description("True when: below the threshold but within the margin percentage of it (structuring). Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }

            [Description("True when: below the threshold but within the margin percentage of it (structuring). Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }

            [Description("True when: below the threshold but within the margin percentage of it (structuring). Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsJustBelowThreshold(flow.Value, threshold, marginPercent));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: below the threshold but within the margin percentage of it (structuring). Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return value.Start().MatchIsJustBelowThreshold(threshold, marginPercent);
            }

            [Description("Begins a pipeline from the value. True when: below the threshold but within the margin percentage of it (structuring). Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return value.Start().RejectIsJustBelowThreshold(threshold, marginPercent);
            }

            [Description("Begins a pipeline from the value. True when: below the threshold but within the margin percentage of it (structuring). Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return value.Start().BreakIsJustBelowThreshold(threshold, marginPercent);
            }

            [Description("Begins a pipeline from the value. True when: below the threshold but within the margin percentage of it (structuring). Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return value.Start().RequireIsJustBelowThreshold(threshold, marginPercent);
            }

            [Description("Begins a pipeline from the value. True when: below the threshold but within the margin percentage of it (structuring). Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsJustBelowThreshold([Description("The threshold (e.g. a reporting limit).")] double threshold,
                [Description("The margin below the threshold, as a percentage of it.")] double marginPercent)
            {
                return value.Start().EnsureIsJustBelowThreshold(threshold, marginPercent);
            }
        }
    }
}

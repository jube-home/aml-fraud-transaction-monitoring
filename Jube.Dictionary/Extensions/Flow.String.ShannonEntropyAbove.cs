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
        extension(Flow<string?> flow)
        {
            [Description("True when: non-empty and entropy above the threshold (generated looking values). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringShannonEntropyAbove(flow.Value, threshold));
            }

            [Description("True when: non-empty and entropy above the threshold (generated looking values). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringShannonEntropyAbove(flow.Value, threshold));
            }

            [Description("True when: non-empty and entropy above the threshold (generated looking values). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringShannonEntropyAbove(flow.Value, threshold));
            }

            [Description("True when: non-empty and entropy above the threshold (generated looking values). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringShannonEntropyAbove(flow.Value, threshold));
            }

            [Description("True when: non-empty and entropy above the threshold (generated looking values). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringShannonEntropyAbove(flow.Value, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: non-empty and entropy above the threshold (generated looking values). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().MatchShannonEntropyAbove(threshold);
            }

            [Description("Begins a pipeline from the value. True when: non-empty and entropy above the threshold (generated looking values). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().RejectShannonEntropyAbove(threshold);
            }

            [Description("Begins a pipeline from the value. True when: non-empty and entropy above the threshold (generated looking values). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().BreakShannonEntropyAbove(threshold);
            }

            [Description("Begins a pipeline from the value. True when: non-empty and entropy above the threshold (generated looking values). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().RequireShannonEntropyAbove(threshold);
            }

            [Description("Begins a pipeline from the value. True when: non-empty and entropy above the threshold (generated looking values). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureShannonEntropyAbove([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().EnsureShannonEntropyAbove(threshold);
            }
        }
    }
}

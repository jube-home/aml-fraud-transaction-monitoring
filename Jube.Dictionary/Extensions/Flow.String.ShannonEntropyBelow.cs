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
            [Description("True when: non-empty and entropy below the threshold. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }

            [Description("True when: non-empty and entropy below the threshold. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }

            [Description("True when: non-empty and entropy below the threshold. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }

            [Description("True when: non-empty and entropy below the threshold. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }

            [Description("True when: non-empty and entropy below the threshold. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: non-empty and entropy below the threshold. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().MatchShannonEntropyBelow(threshold);
            }

            [Description("Begins a pipeline from the value. True when: non-empty and entropy below the threshold. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().RejectShannonEntropyBelow(threshold);
            }

            [Description("Begins a pipeline from the value. True when: non-empty and entropy below the threshold. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().BreakShannonEntropyBelow(threshold);
            }

            [Description("Begins a pipeline from the value. True when: non-empty and entropy below the threshold. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().RequireShannonEntropyBelow(threshold);
            }

            [Description("Begins a pipeline from the value. True when: non-empty and entropy below the threshold. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureShannonEntropyBelow([Description("The entropy in bits the value is tested against.")] double threshold)
            {
                return value.Start().EnsureShannonEntropyBelow(threshold);
            }
        }
    }
}

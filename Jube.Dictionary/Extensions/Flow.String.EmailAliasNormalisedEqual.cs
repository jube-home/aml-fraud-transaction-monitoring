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
            [Description("True when: equal to the argument after removing case and any plus tag (multi-accounting). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }

            [Description("True when: equal to the argument after removing case and any plus tag (multi-accounting). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }

            [Description("True when: equal to the argument after removing case and any plus tag (multi-accounting). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }

            [Description("True when: equal to the argument after removing case and any plus tag (multi-accounting). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }

            [Description("True when: equal to the argument after removing case and any plus tag (multi-accounting). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringEmailAliasNormalisedEqual(flow.Value, other));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: equal to the argument after removing case and any plus tag (multi-accounting). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return value.Start().MatchEmailAliasNormalisedEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: equal to the argument after removing case and any plus tag (multi-accounting). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return value.Start().RejectEmailAliasNormalisedEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: equal to the argument after removing case and any plus tag (multi-accounting). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return value.Start().BreakEmailAliasNormalisedEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: equal to the argument after removing case and any plus tag (multi-accounting). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return value.Start().RequireEmailAliasNormalisedEqual(other);
            }

            [Description("Begins a pipeline from the value. True when: equal to the argument after removing case and any plus tag (multi-accounting). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureEmailAliasNormalisedEqual([Description("The email address the value is compared with.")] string? other)
            {
                return value.Start().EnsureEmailAliasNormalisedEqual(other);
            }
        }
    }
}

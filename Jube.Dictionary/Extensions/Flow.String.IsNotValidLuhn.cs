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
            [Description("True when: null or fails the Luhn checksum. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNotValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotValidLuhn(flow.Value));
            }

            [Description("True when: null or fails the Luhn checksum. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNotValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotValidLuhn(flow.Value));
            }

            [Description("True when: null or fails the Luhn checksum. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNotValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotValidLuhn(flow.Value));
            }

            [Description("True when: null or fails the Luhn checksum. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNotValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotValidLuhn(flow.Value));
            }

            [Description("True when: null or fails the Luhn checksum. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNotValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotValidLuhn(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: null or fails the Luhn checksum. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsNotValidLuhn()
            {
                return value.Start().MatchIsNotValidLuhn();
            }

            [Description("Begins a pipeline from the value. True when: null or fails the Luhn checksum. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsNotValidLuhn()
            {
                return value.Start().RejectIsNotValidLuhn();
            }

            [Description("Begins a pipeline from the value. True when: null or fails the Luhn checksum. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsNotValidLuhn()
            {
                return value.Start().BreakIsNotValidLuhn();
            }

            [Description("Begins a pipeline from the value. True when: null or fails the Luhn checksum. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsNotValidLuhn()
            {
                return value.Start().RequireIsNotValidLuhn();
            }

            [Description("Begins a pipeline from the value. True when: null or fails the Luhn checksum. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsNotValidLuhn()
            {
                return value.Start().EnsureIsNotValidLuhn();
            }
        }
    }
}

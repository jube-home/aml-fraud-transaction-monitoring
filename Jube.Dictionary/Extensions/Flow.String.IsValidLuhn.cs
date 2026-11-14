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
            [Description("True when: passes the Luhn checksum. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsValidLuhn(flow.Value));
            }

            [Description("True when: passes the Luhn checksum. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsValidLuhn(flow.Value));
            }

            [Description("True when: passes the Luhn checksum. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsValidLuhn(flow.Value));
            }

            [Description("True when: passes the Luhn checksum. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsValidLuhn(flow.Value));
            }

            [Description("True when: passes the Luhn checksum. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidLuhn()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsValidLuhn(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: passes the Luhn checksum. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsValidLuhn()
            {
                return value.Start().MatchIsValidLuhn();
            }

            [Description("Begins a pipeline from the value. True when: passes the Luhn checksum. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsValidLuhn()
            {
                return value.Start().RejectIsValidLuhn();
            }

            [Description("Begins a pipeline from the value. True when: passes the Luhn checksum. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsValidLuhn()
            {
                return value.Start().BreakIsValidLuhn();
            }

            [Description("Begins a pipeline from the value. True when: passes the Luhn checksum. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsValidLuhn()
            {
                return value.Start().RequireIsValidLuhn();
            }

            [Description("Begins a pipeline from the value. True when: passes the Luhn checksum. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsValidLuhn()
            {
                return value.Start().EnsureIsValidLuhn();
            }
        }
    }
}

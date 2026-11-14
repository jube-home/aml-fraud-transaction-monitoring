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
            [Description("True when: non-empty, letters and digits only. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }

            [Description("True when: non-empty, letters and digits only. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }

            [Description("True when: non-empty, letters and digits only. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }

            [Description("True when: non-empty, letters and digits only. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }

            [Description("True when: non-empty, letters and digits only. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: non-empty, letters and digits only. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsAlphaNumeric()
            {
                return value.Start().MatchIsAlphaNumeric();
            }

            [Description("Begins a pipeline from the value. True when: non-empty, letters and digits only. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsAlphaNumeric()
            {
                return value.Start().RejectIsAlphaNumeric();
            }

            [Description("Begins a pipeline from the value. True when: non-empty, letters and digits only. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsAlphaNumeric()
            {
                return value.Start().BreakIsAlphaNumeric();
            }

            [Description("Begins a pipeline from the value. True when: non-empty, letters and digits only. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsAlphaNumeric()
            {
                return value.Start().RequireIsAlphaNumeric();
            }

            [Description("Begins a pipeline from the value. True when: non-empty, letters and digits only. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsAlphaNumeric()
            {
                return value.Start().EnsureIsAlphaNumeric();
            }
        }
    }
}

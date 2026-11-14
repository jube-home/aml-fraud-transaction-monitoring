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
            [Description("True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }

            [Description("True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }

            [Description("True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }

            [Description("True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }

            [Description("True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure,
                        FlowPredicates.StringIsDelimitedMember(flow.Value, delimitedList, separator));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return value.Start().MatchIsDelimitedMember(delimitedList, separator);
            }

            [Description("Begins a pipeline from the value. True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return value.Start().RejectIsDelimitedMember(delimitedList, separator);
            }

            [Description("Begins a pipeline from the value. True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return value.Start().BreakIsDelimitedMember(delimitedList, separator);
            }

            [Description("Begins a pipeline from the value. True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return value.Start().RequireIsDelimitedMember(delimitedList, separator);
            }

            [Description("Begins a pipeline from the value. True when: equal, after trimming, to one of the entries of the delimited list split on the separator. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureIsDelimitedMember([Description("The delimited text whose entries are searched.")] string? delimitedList,
                [Description("The text that separates the entries.")] string? separator)
            {
                return value.Start().EnsureIsDelimitedMember(delimitedList, separator);
            }
        }
    }
}
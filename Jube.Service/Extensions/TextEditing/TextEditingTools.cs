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

using System.ComponentModel;
using System.Globalization;
using Jube.Dictionary.Extensions;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;

namespace Jube.Service.Extensions.TextEditing
{
    [FunctionToolGroup("TextEditing",
        "Edits text exactly: case, reversing, repeating, truncating, taking the left or right characters, masking and replacing the first or last occurrence.")]
    public static class TextEditingTools
    {
        [FunctionTool("ToTitleCase")]
        [Description(
            "Capitalises the first letter of each word and lowercases the rest, except words already all in capitals, which are kept as acronyms; 'john SMITH-jones' becomes 'John SMITH-Jones'.")]
        public static string ToTitleCase([Description("The text to convert.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).ToTitleCase(CultureInfo.InvariantCulture);
        }

        [FunctionTool("ReverseText")]
        [Description("Reverses the order of the characters in a text; 'abc' becomes 'cba'.")]
        public static string ReverseText([Description("The text to reverse.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).Reverse();
        }

        [FunctionTool("RepeatText")]
        [Description(
            "Repeats a text a number of times end to end; 'ab' three times is 'ababab'. The result is limited to 100000 characters.")]
        public static string RepeatText(
            [Description("The text to repeat.")] string text,
            [Description("How many copies, zero or more.")]
            int count)
        {
            var value = FunctionToolInput.Text(text, nameof(text));

            if (count < 0 || (long)count * value.Length > FunctionToolInput.MaximumTextLength)
            {
                throw new ArgumentException(
                    $"count must be zero or more and the result at most {FunctionToolInput.MaximumTextLength} characters.",
                    nameof(count));
            }

            return value.Repeat(count);
        }

        [FunctionTool("TruncateText")]
        [Description(
            "Shortens a text to at most a maximum length, ending with a suffix (default ...) when it was cut; texts already short enough are returned unchanged.")]
        public static string TruncateText(
            [Description("The text to shorten.")] string text,
            [Description("The maximum length of the result, including the suffix.")]
            int maximumLength,
            [Description("Text appended when cut, such as ... (default).")]
            string suffix = "...")
        {
            var value = FunctionToolInput.Text(text, nameof(text));
            var ending = FunctionToolInput.Text(suffix, nameof(suffix), 100);

            if (maximumLength < ending.Length)
            {
                throw new ArgumentException("maximumLength must be at least the length of the suffix.",
                    nameof(maximumLength));
            }

            return value.Truncate(maximumLength, ending) ?? "";
        }

        [FunctionTool("TakeFirstCharacters")]
        [Description(
            "The first n characters of a text, or the whole text if it is shorter; the first 4 of 'GB82WEST' is 'GB82'.")]
        public static string TakeFirstCharacters(
            [Description("The text to take from.")]
            string text,
            [Description("How many characters to keep, zero or more.")]
            int count)
        {
            return FunctionToolInput.Text(text, nameof(text)).LeftSafe(NonNegative(count));
        }

        [FunctionTool("TakeLastCharacters")]
        [Description(
            "The last n characters of a text, or the whole text if it is shorter; the last 4 of '4111111111111111' is '1111'.")]
        public static string TakeLastCharacters(
            [Description("The text to take from.")]
            string text,
            [Description("How many characters to keep, zero or more.")]
            int count)
        {
            return FunctionToolInput.Text(text, nameof(text)).RightSafe(NonNegative(count));
        }

        [FunctionTool("MaskAllButLast")]
        [Description(
            "Replaces all but the last few characters with a mask character, for safe display of card numbers, accounts and phone numbers; '4111111111111111' with 4 visible is '************1111'.")]
        public static string MaskAllButLast(
            [Description("The text to mask.")] string text,
            [Description("How many trailing characters stay visible.")]
            int visibleCount = 4,
            [Description("The single masking character, * by default.")]
            string mask = "*")
        {
            if (string.IsNullOrEmpty(mask) || mask.Length != 1)
            {
                throw new ArgumentException("mask must be exactly one character.", nameof(mask));
            }

            return FunctionToolInput.Text(text, nameof(text)).MaskExceptLast(NonNegative(visibleCount), mask[0]);
        }

        [FunctionTool("ReplaceFirstOccurrence")]
        [Description(
            "Replaces only the first case-sensitive occurrence of a substring; texts without it are returned unchanged.")]
        public static string ReplaceFirstOccurrence(
            [Description("The text to edit.")] string text,
            [Description("The substring to find; must not be empty.")]
            string oldValue,
            [Description("The replacement text, which may be empty.")]
            string newValue)
        {
            return FunctionToolInput.Text(text, nameof(text))
                .ReplaceFirst(FunctionToolInput.Required(oldValue, nameof(oldValue)),
                    FunctionToolInput.Text(newValue, nameof(newValue)));
        }

        [FunctionTool("ReplaceLastOccurrence")]
        [Description(
            "Replaces only the last case-sensitive occurrence of a substring; texts without it are returned unchanged.")]
        public static string ReplaceLastOccurrence(
            [Description("The text to edit.")] string text,
            [Description("The substring to find; must not be empty.")]
            string oldValue,
            [Description("The replacement text, which may be empty.")]
            string newValue)
        {
            return FunctionToolInput.Text(text, nameof(text))
                .ReplaceLast(FunctionToolInput.Required(oldValue, nameof(oldValue)),
                    FunctionToolInput.Text(newValue, nameof(newValue)));
        }

        private static int NonNegative(int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("count must be zero or more.", nameof(count));
            }

            return count;
        }
    }
}
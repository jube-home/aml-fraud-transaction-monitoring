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
using Jube.Dictionary.Extensions;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Extensions.TextInspection.Models;

namespace Jube.Service.Extensions.TextInspection
{
    [FunctionToolGroup("TextInspection",
        "Counts and tests on text: words, lines, letters, digits, character variety, entropy, repeated or sequential characters, palindromes, anagrams and containment.")]
    public static class TextInspectionTools
    {
        [FunctionTool("DescribeText")]
        [Description(
            "Measures a text in one call: characters, words, lines, letters, digits, distinct characters, longest run of one repeated character, Shannon entropy in bits per character, and whether it is all ASCII letters, all ASCII digits, or one repeated character. Low entropy or long runs suggest synthetic or keyboard-mashed identifiers.")]
        public static TextMeasures DescribeText([Description("The text to measure.")] string text)
        {
            var value = FunctionToolInput.Text(text, nameof(text));

            return new TextMeasures(
                value.Length,
                value.WordCount(),
                value.LineCount(),
                value.LetterCount(),
                value.DigitCount(),
                value.DistinctCharacterCount(),
                value.LongestCharacterRun(),
                value.ShannonEntropy(),
                value.Length > 0 && value.IsAlpha(),
                value.Length > 0 && value.IsNumeric(),
                value.Length > 0 && value.IsAllSameCharacter());
        }

        [FunctionTool("CountOccurrences")]
        [Description(
            "Counts non-overlapping, case-sensitive occurrences of a substring in a text; 'aba' occurs twice in 'abababa'.")]
        public static int CountOccurrences(
            [Description("The text to search.")] string text,
            [Description("The substring to count; must not be empty.")]
            string value)
        {
            return FunctionToolInput.Text(text, nameof(text))
                .CountOccurrences(FunctionToolInput.Required(value, nameof(value)));
        }

        [FunctionTool("HasSequentialDigits")]
        [Description(
            "Whether a text contains a run of consecutive ascending or descending digits of at least a given length, such as 1234 or 9876. Common in weak PINs, test card numbers and fabricated identifiers.")]
        public static bool HasSequentialDigits(
            [Description("The text to scan.")] string text,
            [Description("Shortest run that counts, at least 2, such as 4.")]
            int minimumRunLength)
        {
            return FunctionToolInput.Text(text, nameof(text)).HasSequentialDigits(minimumRunLength);
        }

        [FunctionTool("IsPalindrome")]
        [Description(
            "Whether a text reads the same backwards, ignoring case, spaces and punctuation; 'A man, a plan, a canal: Panama' is a palindrome.")]
        public static bool IsPalindrome([Description("The text to test.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).IsPalindrome();
        }

        [FunctionTool("AreAnagrams")]
        [Description(
            "Whether two texts use exactly the same characters the same number of times, case-sensitive and counting spaces; 'listen' and 'silent' are anagrams. Normalise case first if case should not matter.")]
        public static bool AreAnagrams(
            [Description("The first text.")] string first,
            [Description("The second text.")] string second)
        {
            return FunctionToolInput.Text(first, nameof(first))
                .IsAnagram(FunctionToolInput.Text(second, nameof(second)));
        }

        [FunctionTool("EqualsIgnoringCase")]
        [Description(
            "Whether two texts are equal when upper and lower case are ignored (ordinal, culture-independent comparison).")]
        public static bool EqualsIgnoringCase(
            [Description("The first text.")] string first,
            [Description("The second text.")] string second)
        {
            return FunctionToolInput.Text(first, nameof(first))
                .EqualsIgnoreCase(FunctionToolInput.Text(second, nameof(second)));
        }

        [FunctionTool("ContainsAllOf")]
        [Description(
            "Whether a text contains every one of several substrings, optionally ignoring case. Use it for keyword checks in narratives and references.")]
        public static bool ContainsAllOf(
            [Description("The text to search.")] string text,
            [Description("The substrings that must all be present.")]
            string[] values,
            [Description("True to ignore upper and lower case.")]
            bool ignoreCase = false)
        {
            return FunctionToolInput.Text(text, nameof(text)).ContainsAll(Comparison(ignoreCase),
                FunctionToolInput.List(values, nameof(values)));
        }

        [FunctionTool("ContainsAnyOf")]
        [Description(
            "Whether a text contains at least one of several substrings, optionally ignoring case. Use it for keyword or watch-word checks.")]
        public static bool ContainsAnyOf(
            [Description("The text to search.")] string text,
            [Description("The substrings of which at least one must be present.")]
            string[] values,
            [Description("True to ignore upper and lower case.")]
            bool ignoreCase = false)
        {
            return FunctionToolInput.Text(text, nameof(text)).ContainsAny(Comparison(ignoreCase),
                FunctionToolInput.List(values, nameof(values)));
        }

        private static StringComparison Comparison(bool ignoreCase)
        {
            return ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }
    }
}
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

namespace Jube.Service.Extensions.TextExtraction
{
    [FunctionToolGroup("TextExtraction",
        "Pulls pieces out of text: the part before, after or between markers, the n-th token, all tokens, and numbers written inside free text.")]
    public static class TextExtractionTools
    {
        [FunctionTool("TextAfter")]
        [Description(
            "The part of a text after the first occurrence of a marker, or empty if the marker is absent; after '@' in 'ann@example.com' is 'example.com'.")]
        public static string TextAfter(
            [Description("The text to search.")] string text,
            [Description("The marker to look for; must not be empty.")]
            string marker)
        {
            return FunctionToolInput.Text(text, nameof(text))
                .GetAfter(FunctionToolInput.Required(marker, nameof(marker)));
        }

        [FunctionTool("TextBefore")]
        [Description(
            "The part of a text before the first occurrence of a marker, or empty if the marker is absent; before '@' in 'ann@example.com' is 'ann'.")]
        public static string TextBefore(
            [Description("The text to search.")] string text,
            [Description("The marker to look for; must not be empty.")]
            string marker)
        {
            return FunctionToolInput.Text(text, nameof(text))
                .GetBefore(FunctionToolInput.Required(marker, nameof(marker)));
        }

        [FunctionTool("TextBetween")]
        [Description(
            "The text between the first occurrence of a start marker and the next end marker after it, or empty if either is missing; between 'ref:' and ';' in 'x ref:AB12; y' is 'AB12'.")]
        public static string TextBetween(
            [Description("The text to search.")] string text,
            [Description("The marker that comes before the wanted text.")]
            string startMarker,
            [Description("The marker that comes after the wanted text.")]
            string endMarker)
        {
            return FunctionToolInput.Text(text, nameof(text))
                .GetBetween(FunctionToolInput.Required(startMarker, nameof(startMarker)),
                    FunctionToolInput.Required(endMarker, nameof(endMarker)));
        }

        [FunctionTool("TokenAt")]
        [Description(
            "The token at a zero-based position after splitting on a separator; a negative position counts from the end, so -1 is the last. Out-of-range positions give empty text.")]
        public static string TokenAt(
            [Description("The text to split.")] string text,
            [Description("The separator, such as a comma, pipe or space; a space if empty.")]
            string separator,
            [Description("Zero-based position; negative counts from the end.")]
            int index)
        {
            return FunctionToolInput.Text(text, nameof(text))
                .TokenAt(FunctionToolInput.Text(separator, nameof(separator), 100), index);
        }

        [FunctionTool("SplitText")]
        [Description(
            "Splits a text on a separator into a list, optionally trimming each part and dropping empty parts. Returns at most 10000 parts.")]
        public static string[] SplitText(
            [Description("The text to split.")] string text,
            [Description("The separator; must not be empty.")]
            string separator,
            [Description("True to trim spaces around each part.")]
            bool trim = true,
            [Description("True to drop parts that are empty.")]
            bool removeEmpty = false)
        {
            var options = (trim ? StringSplitOptions.TrimEntries : StringSplitOptions.None) |
                          (removeEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);

            var parts = FunctionToolInput.Text(text, nameof(text))
                .Split(FunctionToolInput.Required(separator, nameof(separator), 100), options);

            return parts.Length > FunctionToolInput.MaximumListLength
                ? parts[..FunctionToolInput.MaximumListLength]
                : parts;
        }

        [FunctionTool("ExtractNumbers")]
        [Description(
            "Every decimal number written in a free text, in order, reading '.' as the decimal point and a leading '-' as negative; 'paid 12.50 then -3 twice' gives 12.5, -3. Thousands separators split numbers, so remove them first.")]
        public static double[] ExtractNumbers([Description("The text to read numbers from.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).ExtractManyDouble();
        }

        [FunctionTool("ExtractWholeNumbers")]
        [Description(
            "Every whole number written in a free text, in order, with a leading '-' read as negative; 'invoices 1001 and 1002' gives 1001, 1002. Digit runs too long for a 64-bit integer are refused.")]
        public static long[] ExtractWholeNumbers([Description("The text to read whole numbers from.")] string text)
        {
            try
            {
                return FunctionToolInput.Text(text, nameof(text)).ExtractManyInt64();
            }
            catch (OverflowException)
            {
                throw new ArgumentException("A digit run is too long for a 64-bit integer.", nameof(text));
            }
        }

        [FunctionTool("QueryStringValue")]
        [Description(
            "The decoded value of a named parameter in a URL or query string, or empty if absent; name in 'https://x.test/p?name=J%C3%BCbe+Ltd' is 'Jübe Ltd'.")]
        public static string QueryStringValue(
            [Description("A full URL or just the query string.")]
            string urlOrQuery,
            [Description("The parameter name, case-sensitive.")]
            string name)
        {
            return FunctionToolInput.Text(urlOrQuery, nameof(urlOrQuery))
                .QueryStringValue(FunctionToolInput.Required(name, nameof(name), 1000));
        }
    }
}
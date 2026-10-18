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

namespace Jube.Service.Extensions.TextCleaning
{
    [FunctionToolGroup("TextCleaning",
        "Cleans and normalises text before comparison or storage: accents, whitespace, control characters, Unicode forms, keeping or removing letters and digits, and URL slugs.")]
    public static class TextCleaningTools
    {
        [FunctionTool("RemoveAccents")]
        [Description(
            "Removes accents and other combining marks while keeping the base letters; 'Crème Brûlée' becomes 'Creme Brulee'. Use it before comparing names from different systems.")]
        public static string RemoveAccents([Description("The text to clean.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).RemoveDiacritics();
        }

        [FunctionTool("CollapseWhitespace")]
        [Description("Trims the text and replaces every run of spaces, tabs and line breaks with a single space.")]
        public static string CollapseWhitespace([Description("The text to clean.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).CollapseWhitespace();
        }

        [FunctionTool("RemoveControlCharacters")]
        [Description(
            "Removes invisible control characters such as NUL and BEL, keeping tabs and line breaks. Use it on data pasted from other systems before display or matching.")]
        public static string RemoveControlCharacters([Description("The text to clean.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).RemoveControlCharacters();
        }

        [FunctionTool("NormaliseUnicode")]
        [Description(
            "Applies a Unicode normalisation form: NFC composes accents (the usual storage form), NFD decomposes them, NFKC and NFKD also fold compatibility characters such as the 'fi' ligature and full-width letters to their plain forms, which defeats look-alike tricks.")]
        public static string NormaliseUnicode(
            [Description("The text to normalise.")]
            string text,
            [Description("The form: NFC (default), NFD, NFKC or NFKD.")]
            string form = "NFC")
        {
            return FunctionToolInput.Text(text, nameof(text)).UnicodeNormalised(form);
        }

        [FunctionTool("KeepOnlyDigits")]
        [Description(
            "Keeps only the numeric characters of a text, dropping everything else; '+44 (0) 7911-123456' gives '4407911123456'.")]
        public static string KeepOnlyDigits([Description("The text to filter.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).ExtractNumber();
        }

        [FunctionTool("KeepOnlyLetters")]
        [Description(
            "Keeps only the letters of a text, in any alphabet, dropping digits, spaces and punctuation; 'AB-12 cd' gives 'ABcd'.")]
        public static string KeepOnlyLetters([Description("The text to filter.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).ExtractLetter();
        }

        [FunctionTool("RemoveDigits")]
        [Description("Removes every numeric character from a text, keeping letters, spaces and punctuation.")]
        public static string RemoveDigits([Description("The text to filter.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).RemoveNumber();
        }

        [FunctionTool("RemoveLetters")]
        [Description("Removes every letter from a text, keeping digits, spaces and punctuation.")]
        public static string RemoveLetters([Description("The text to filter.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).RemoveLetter();
        }

        [FunctionTool("ToSlug")]
        [Description(
            "Turns text into a lowercase ASCII slug of letters and digits joined by hyphens; 'Crème Brûlée & Co.!' becomes 'creme-brulee-co'. Useful for stable keys and file-safe names.")]
        public static string ToSlug([Description("The text to convert.")] string text)
        {
            return FunctionToolInput.Text(text, nameof(text)).ToSlug();
        }
    }
}
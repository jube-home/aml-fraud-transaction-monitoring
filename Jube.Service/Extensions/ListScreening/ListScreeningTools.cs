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
using Jube.Service.Extensions.ListScreening.Models;

namespace Jube.Service.Extensions.ListScreening
{
    [FunctionToolGroup("ListScreening",
        "Screens a name or text against a list, such as a watch list or allow list, with configurable fuzzy matching, and reports the best match, its score and how many entries matched.")]
    public static class ListScreeningTools
    {
        [FunctionTool("ScreenAgainstList")]
        [Description(
            "Screens one value against a list with a fuzzy rule and reports whether anything matched, how many entries matched, the best matching entry and its score from 0 to 1. The main tool for checking a name against a watch list or a counterparty against known entities.")]
        public static ScreeningResult ScreenAgainstList(
            [Description("The name or text to screen.")]
            string value,
            [Description("The list entries to screen against, up to 10000.")]
            string[] list,
            [Description(
                "Comma-separated fuzzy options, for example jarowinkler=0.9,tokenset or damerau=1,soundex,notitles,nosuffixes; empty uses jarowinkler=0.92,tokensort=0.92 on case-, accent- and punctuation-folded text. Algorithms: exact, levenshtein, damerau, similarity, jarowinkler, dice, soundex, tokensort, tokenset, initials; modifiers: case, diacritics, punct, nospaces, notitles, nosuffixes, within, all, any, firstletter, minlength=n.")]
            string options = "")
        {
            var text = FunctionToolInput.Text(value, nameof(value), FunctionToolInput.MaximumNameLength);
            var entries = FunctionToolInput.List(list, nameof(list));
            var checkedOptions = FuzzyOptionsText.Checked(options);
            var count = text.FuzzyMatchCount(entries, checkedOptions);

            return new ScreeningResult(count > 0, count, text.FuzzyBestMatch(entries, checkedOptions),
                text.FuzzyBestScore(entries, checkedOptions));
        }

        [FunctionTool("IsFuzzilyInList")]
        [Description(
            "Whether a value fuzzily equals any entry of a list under a fuzzy rule. Use ScreenAgainstList when the matching entry or score is also wanted.")]
        public static bool IsFuzzilyInList(
            [Description("The name or text to look for.")]
            string value,
            [Description("The list entries, up to 10000.")]
            string[] list,
            [Description(
                "Comma-separated fuzzy options, for example jarowinkler=0.9,tokenset or damerau=1,soundex,notitles,nosuffixes; empty uses jarowinkler=0.92,tokensort=0.92 on case-, accent- and punctuation-folded text. Algorithms: exact, levenshtein, damerau, similarity, jarowinkler, dice, soundex, tokensort, tokenset, initials; modifiers: case, diacritics, punct, nospaces, notitles, nosuffixes, within, all, any, firstletter, minlength=n.")]
            string options = "")
        {
            return FunctionToolInput.Text(value, nameof(value), FunctionToolInput.MaximumNameLength)
                .IsSomehowInList(FunctionToolInput.List(list, nameof(list)), FuzzyOptionsText.Checked(options));
        }

        [FunctionTool("ContainsFuzzilyAnyInList")]
        [Description(
            "Whether any list entry fuzzily appears inside a longer text, such as a sanctioned name inside a payment narrative. The within modifier is applied automatically.")]
        public static bool ContainsFuzzilyAnyInList(
            [Description("The longer text to search, such as a payment reference.")]
            string text,
            [Description("The list entries to look for, up to 10000.")]
            string[] list,
            [Description(
                "Comma-separated fuzzy options, for example jarowinkler=0.9,tokenset or damerau=1,soundex,notitles,nosuffixes; empty uses jarowinkler=0.92,tokensort=0.92 on case-, accent- and punctuation-folded text. Algorithms: exact, levenshtein, damerau, similarity, jarowinkler, dice, soundex, tokensort, tokenset, initials; modifiers: case, diacritics, punct, nospaces, notitles, nosuffixes, within, all, any, firstletter, minlength=n.")]
            string options = "")
        {
            return FunctionToolInput.Text(text, nameof(text), 10_000)
                .ContainsSomehowInList(FunctionToolInput.List(list, nameof(list)), FuzzyOptionsText.Checked(options));
        }

        [FunctionTool("IsExactlyInList")]
        [Description(
            "Whether a value equals any list entry exactly, optionally ignoring case; no fuzziness. Use it for code lists such as country or merchant category codes.")]
        public static bool IsExactlyInList(
            [Description("The value to look for.")]
            string value,
            [Description("The list entries, up to 10000.")]
            string[] list,
            [Description("True to ignore upper and lower case.")]
            bool ignoreCase = false)
        {
            var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            return FunctionToolInput.List(list, nameof(list))
                .Contains(FunctionToolInput.Text(value, nameof(value)), comparer);
        }

        [FunctionTool("InitialsMatchInList")]
        [Description(
            "Whether a name matches any list entry when initials are allowed to stand for whole words; 'J R R Tolkien' matches 'John Ronald Reuel Tolkien'.")]
        public static bool InitialsMatchInList(
            [Description("The name to look for.")] string value,
            [Description("The list entries, up to 10000.")]
            string[] list)
        {
            return FunctionToolInput.Text(value, nameof(value), FunctionToolInput.MaximumNameLength)
                .IsInitialsInList(FunctionToolInput.List(list, nameof(list)));
        }

        [FunctionTool("SoundsLikeAnyInList")]
        [Description(
            "Whether a name sounds like any list entry word by word under Soundex after case and accent folding. Phonetic only, so pair it with an edit-distance check.")]
        public static bool SoundsLikeAnyInList(
            [Description("The name to look for.")] string value,
            [Description("The list entries, up to 10000.")]
            string[] list)
        {
            return FunctionToolInput.Text(value, nameof(value), FunctionToolInput.MaximumNameLength)
                .IsSoundexInList(FunctionToolInput.List(list, nameof(list)));
        }

        [FunctionTool("CheckFuzzyOptions")]
        [Description(
            "Checks a fuzzy options string and returns an empty text when it is valid, or the reason it is not. Use it before saving a list-matching rule.")]
        public static string CheckFuzzyOptions(
            [Description(
                "Comma-separated fuzzy options, for example jarowinkler=0.9,tokenset or damerau=1,soundex,notitles,nosuffixes; empty uses jarowinkler=0.92,tokensort=0.92 on case-, accent- and punctuation-folded text. Algorithms: exact, levenshtein, damerau, similarity, jarowinkler, dice, soundex, tokensort, tokenset, initials; modifiers: case, diacritics, punct, nospaces, notitles, nosuffixes, within, all, any, firstletter, minlength=n.")]
            string options)
        {
            return FunctionToolInput.Text(options, nameof(options), 1000).FuzzyOptionsError();
        }
    }
}
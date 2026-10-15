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
using Jube.Service.Extensions.NameSimilarity.Models;

namespace Jube.Service.Extensions.NameSimilarity
{
    [FunctionToolGroup("NameSimilarity",
        "Scores how alike two names or references are with edit-distance, phonetic and token algorithms, and normalises names for matching; for pairwise comparison rather than screening a list.")]
    public static class NameSimilarityTools
    {
        [FunctionTool("CompareNames")]
        [Description(
            "Scores two names with every similarity algorithm at once: Levenshtein and Damerau edit distances, normalised similarity, Jaro-Winkler, Dice bigrams, token-sort, token-set (0 to 1, higher is closer), and whether they sound alike by Soundex. Use it to explain why two parties may or may not be the same.")]
        public static NameComparison CompareNames(
            [Description("The first name or reference.")]
            string first,
            [Description("The second name or reference.")]
            string second)
        {
            var a = FunctionToolInput.Text(first, nameof(first), FunctionToolInput.MaximumNameLength);
            var b = FunctionToolInput.Text(second, nameof(second), FunctionToolInput.MaximumNameLength);

            return new NameComparison(
                a.LevenshteinDistance(b),
                a.DamerauDistance(b),
                a.SimilarityTo(b),
                a.JaroWinklerSimilarity(b),
                a.DiceSimilarity(b),
                a.TokenSortSimilarity(b),
                a.TokenSetSimilarity(b),
                a.SoundsLike(b));
        }

        [FunctionTool("LevenshteinDistance")]
        [Description(
            "Minimum number of single-character insertions, deletions or substitutions turning one text into another, case-sensitive; 'kitten' to 'sitting' is 3.")]
        public static int LevenshteinDistance(
            [Description("The first text.")] string first,
            [Description("The second text.")] string second)
        {
            return FunctionToolInput.Text(first, nameof(first), FunctionToolInput.MaximumNameLength)
                .LevenshteinDistance(
                    FunctionToolInput.Text(second, nameof(second), FunctionToolInput.MaximumNameLength));
        }

        [FunctionTool("DamerauDistance")]
        [Description(
            "Edit distance that also counts swapping two adjacent characters as one edit, so typing transpositions score 1; 'abcd' to 'abdc' is 1.")]
        public static int DamerauDistance(
            [Description("The first text.")] string first,
            [Description("The second text.")] string second)
        {
            return FunctionToolInput.Text(first, nameof(first), FunctionToolInput.MaximumNameLength)
                .DamerauDistance(FunctionToolInput.Text(second, nameof(second), FunctionToolInput.MaximumNameLength));
        }

        [FunctionTool("JaroWinklerSimilarity")]
        [Description(
            "Jaro-Winkler similarity from 0 to 1, rewarding a shared prefix; well suited to short personal names. Case-sensitive; 0.92 and above is a typical match threshold.")]
        public static double JaroWinklerSimilarity(
            [Description("The first text.")] string first,
            [Description("The second text.")] string second)
        {
            return FunctionToolInput.Text(first, nameof(first), FunctionToolInput.MaximumNameLength)
                .JaroWinklerSimilarity(FunctionToolInput.Text(second, nameof(second),
                    FunctionToolInput.MaximumNameLength));
        }

        [FunctionTool("SoundexCode")]
        [Description(
            "The four-character American Soundex code of a word, grouping names that sound alike; Robert and Rupert are both R163.")]
        public static string SoundexCode([Description("The word or surname to encode.")] string word)
        {
            return FunctionToolInput.Text(word, nameof(word), FunctionToolInput.MaximumNameLength).Soundex();
        }

        [FunctionTool("NamesSoundAlike")]
        [Description(
            "Whether two names sound alike word by word under Soundex after case and accent folding; 'Robert Smith' and 'Rupert Smyth' do.")]
        public static bool NamesSoundAlike(
            [Description("The first name.")] string first,
            [Description("The second name.")] string second)
        {
            return FunctionToolInput.Text(first, nameof(first), FunctionToolInput.MaximumNameLength)
                .SoundsLike(FunctionToolInput.Text(second, nameof(second), FunctionToolInput.MaximumNameLength));
        }

        [FunctionTool("InitialsOf")]
        [Description("The initials of a name, one uppercase letter per word; 'john ronald reuel tolkien' gives JRRT.")]
        public static string InitialsOf([Description("The name to abbreviate.")] string name)
        {
            return FunctionToolInput.Text(name, nameof(name), FunctionToolInput.MaximumNameLength).InitialsOf();
        }

        [FunctionTool("NormaliseNameForMatching")]
        [Description(
            "The canonical form used for name matching: lowercase, accents and punctuation removed, leading titles (Mr, Dr, Frau) and trailing company suffixes (Ltd, GmbH, LLC) dropped; 'Dr. José Núñez' gives 'jose nunez'.")]
        public static string NormaliseNameForMatching([Description("The personal or company name.")] string name)
        {
            return FunctionToolInput.Text(name, nameof(name), FunctionToolInput.MaximumNameLength)
                .NormalisedForMatching();
        }

        [FunctionTool("NamesMatchWithOptions")]
        [Description(
            "Whether two names match under a configurable fuzzy rule, the same rule language used by list screening, so a pair can be tested before a list rule is written.")]
        public static bool NamesMatchWithOptions(
            [Description("The first name.")] string first,
            [Description("The second name.")] string second,
            [Description(
                "Comma-separated fuzzy options, for example jarowinkler=0.9,tokenset or damerau=1,soundex,notitles,nosuffixes; empty uses jarowinkler=0.92,tokensort=0.92 on case-, accent- and punctuation-folded text. Algorithms: exact, levenshtein, damerau, similarity, jarowinkler, dice, soundex, tokensort, tokenset, initials; modifiers: case, diacritics, punct, nospaces, notitles, nosuffixes, within, all, any, firstletter, minlength=n.")]
            string options = "")
        {
            return FunctionToolInput.Text(first, nameof(first), FunctionToolInput.MaximumNameLength)
                .IsSomehowLike(FunctionToolInput.Text(second, nameof(second), FunctionToolInput.MaximumNameLength),
                    FuzzyOptionsText.Checked(options));
        }
    }
}
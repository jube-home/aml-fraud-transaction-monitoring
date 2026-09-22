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
    internal static partial class FlowPredicates
    {
        internal static bool StringIsNull(string? v)
        {
            return v is null;
        }

        internal static bool StringIsEmpty(string? v)
        {
            return v is not null && v.Length == 0;
        }

        internal static bool StringIsNullOrEmpty(string? v)
        {
            return string.IsNullOrEmpty(v);
        }

        internal static bool StringIsNullOrWhiteSpace(string? v)
        {
            return string.IsNullOrWhiteSpace(v);
        }

        internal static bool StringIsNotNullOrEmpty(string? v)
        {
            return !string.IsNullOrEmpty(v);
        }

        internal static bool StringIsNotNullOrWhiteSpace(string? v)
        {
            return !string.IsNullOrWhiteSpace(v);
        }

        internal static bool StringEqual(string? v, string? other)
        {
            return v is not null && other is not null && string.Equals(v, other, StringComparison.Ordinal);
        }

        internal static bool StringEqualIgnoreCase(string? v, string? other)
        {
            return v is not null && other is not null && string.Equals(v, other, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool StringNotEqual(string? v, string? other)
        {
            return v is not null && other is not null && !string.Equals(v, other, StringComparison.Ordinal);
        }

        internal static bool StringIn(string? v, string[] values)
        {
            return v is not null && Array.IndexOf(values, v) >= 0;
        }

        internal static bool StringInIgnoreCase(string? v, string[] values)
        {
            return v is not null && values.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StringNotIn(string? v, string[] values)
        {
            return v is not null && Array.IndexOf(values, v) < 0;
        }

        internal static bool StringInList(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && list.Contains(v);
        }

        internal static bool StringInListIgnoreCase(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && list.Contains(v, StringComparer.OrdinalIgnoreCase);
        }

        internal static bool StringNotInList(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && !list.Contains(v);
        }

        internal static bool StringContains(string? v, string? substring)
        {
            return v is not null && substring is not null && v.Contains(substring, StringComparison.Ordinal);
        }

        internal static bool StringContainsIgnoreCase(string? v, string? substring)
        {
            return v is not null && substring is not null && v.Contains(substring, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool StringContainsAny(string? v, string[] values)
        {
            return v is not null && values.Any(x => x is not null && v.Contains(x, StringComparison.Ordinal));
        }

        internal static bool StringContainsAnyIgnoreCase(string? v, string[] values)
        {
            return v is not null && values.Any(x => x is not null && v.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StringContainsAll(string? v, string[] values)
        {
            return v is not null && values.All(x => x is not null && v.Contains(x, StringComparison.Ordinal));
        }

        internal static bool StringContainsNone(string? v, string[] values)
        {
            return v is not null && !values.Any(x => x is not null && v.Contains(x, StringComparison.Ordinal));
        }

        internal static bool StringContainsAnyInList(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && list.Any(x => x is not null && v.Contains(x, StringComparison.Ordinal));
        }

        internal static bool StringContainsAnyInListIgnoreCase(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && list.Any(x => x is not null && v.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StringContainsAllInList(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && list.All(x => x is not null && v.Contains(x, StringComparison.Ordinal));
        }

        internal static bool StringContainsNoneInList(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && !list.Any(x => x is not null && v.Contains(x, StringComparison.Ordinal));
        }

        internal static bool StringStartsWith(string? v, string? prefix)
        {
            return v is not null && prefix is not null && v.StartsWith(prefix, StringComparison.Ordinal);
        }

        internal static bool StringEndsWith(string? v, string? suffix)
        {
            return v is not null && suffix is not null && v.EndsWith(suffix, StringComparison.Ordinal);
        }

        internal static bool StringStartsWithAnyInList(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && list.Any(x => x is not null && v.StartsWith(x, StringComparison.Ordinal));
        }

        internal static bool StringMatches(string? v, string? pattern)
        {
            return SafeRegexIsMatch(v, pattern);
        }

        internal static bool StringLengthEqual(string? v, int length)
        {
            return v is not null && v.Length == length;
        }

        internal static bool StringLengthGreater(string? v, int length)
        {
            return v is not null && v.Length > length;
        }

        internal static bool StringLengthLess(string? v, int length)
        {
            return v is not null && v.Length < length;
        }

        internal static bool StringLengthInRange(string? v, int minimum, int maximum)
        {
            return v is not null && v.Length >= minimum && v.Length <= maximum;
        }

        internal static bool StringIsNumeric(string? v)
        {
            return !string.IsNullOrEmpty(v) && v.IsNumeric();
        }

        internal static bool StringIsAlpha(string? v)
        {
            return !string.IsNullOrEmpty(v) && v.IsAlpha();
        }

        internal static bool StringIsAlphaNumeric(string? v)
        {
            return !string.IsNullOrEmpty(v) && v.IsAlphaNumeric();
        }

        internal static bool StringIsAllSameCharacter(string? v)
        {
            return v.IsAllSameCharacter();
        }

        internal static bool StringHasSequentialDigits(string? v, int minimumRunLength)
        {
            return v.HasSequentialDigits(minimumRunLength);
        }

        internal static bool StringShannonEntropyAbove(string? v, double threshold)
        {
            return !string.IsNullOrEmpty(v) && v.ShannonEntropy() > threshold;
        }

        internal static bool StringShannonEntropyBelow(string? v, double threshold)
        {
            return !string.IsNullOrEmpty(v) && v.ShannonEntropy() < threshold;
        }

        internal static bool StringIsValidEmail(string? v)
        {
            return !string.IsNullOrEmpty(v) && v.IsValidEmail();
        }

        internal static bool StringIsNotValidEmail(string? v)
        {
            return string.IsNullOrEmpty(v) || !v.IsValidEmail();
        }

        internal static bool StringIsValidIp(string? v)
        {
            return !string.IsNullOrEmpty(v) && v.IsValidIP();
        }

        internal static bool StringIsNotValidIp(string? v)
        {
            return string.IsNullOrEmpty(v) || !v.IsValidIP();
        }

        internal static bool StringIsValidIban(string? v)
        {
            return v.IsValidIban();
        }

        internal static bool StringIsNotValidIban(string? v)
        {
            return !v.IsValidIban();
        }

        internal static bool StringIsValidBic(string? v)
        {
            return v.IsValidBic();
        }

        internal static bool StringIsNotValidBic(string? v)
        {
            return !v.IsValidBic();
        }

        internal static bool StringIsValidLuhn(string? v)
        {
            return v.IsValidLuhn();
        }

        internal static bool StringIsNotValidLuhn(string? v)
        {
            return !v.IsValidLuhn();
        }

        internal static bool StringIsValidCardExpiry(string? v)
        {
            return v.IsValidCardExpiry();
        }

        internal static bool StringIsNotValidCardExpiry(string? v)
        {
            return !v.IsValidCardExpiry();
        }

        internal static bool StringEmailDomainEqual(string? v, string? domain)
        {
            return v is not null && domain is not null && string.Equals(v.EmailDomain(), domain, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool StringEmailDomainIn(string? v, string[] domains)
        {
            return v is not null && domains.Any(x => string.Equals(v.EmailDomain(), x, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StringEmailDomainInList(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && list.Any(x => string.Equals(v.EmailDomain(), x, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StringEmailAliasNormalisedEqual(string? v, string? other)
        {
            return !string.IsNullOrEmpty(v) && !string.IsNullOrEmpty(other) && v.NormalizeEmailAlias() == other.NormalizeEmailAlias();
        }

        internal static bool StringSoundexEqual(string? v, string? other)
        {
            return !string.IsNullOrEmpty(v) && !string.IsNullOrEmpty(other) && v.Soundex() == other.Soundex();
        }

        internal static bool StringJaroWinklerAtLeast(string? v, string? other, double threshold)
        {
            return v is not null && other is not null && v.JaroWinklerSimilarity(other) >= threshold;
        }

        internal static bool StringSimilarityAtLeast(string? v, string? other, double threshold)
        {
            return v is not null && other is not null && v.SimilarityTo(other) >= threshold;
        }

        internal static bool StringLevenshteinAtMost(string? v, string? other, int distance)
        {
            return v is not null && other is not null && v.LevenshteinDistance(other) <= distance;
        }

        internal static bool StringEndsWithAnyInList(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && list.Any(x => x is not null && v.EndsWith(x, StringComparison.Ordinal));
        }

        internal static bool StringContainsAllIgnoreCase(string? v, string[] values)
        {
            return v is not null && values.All(x => x is not null && v.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StringContainsNoneIgnoreCase(string? v, string[] values)
        {
            return v is not null && !values.Any(x => x is not null && v.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StringContainsAllInListIgnoreCase(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && list.All(x => x is not null && v.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StringContainsNoneInListIgnoreCase(string? v, IEnumerable<string>? list)
        {
            return v is not null && list is not null && !list.Any(x => x is not null && v.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool StringContainsFuzzy(string? v, string? term, double threshold)
        {
            return FlowPredicates.FuzzyContains(v, term, threshold);
        }

        internal static bool StringContainsFuzzyAny(string? v, double threshold, string[] terms)
        {
            return terms.Any(x => FlowPredicates.FuzzyContains(v, x, threshold));
        }

        internal static bool StringContainsFuzzyAnyInList(string? v, IEnumerable<string>? list, double threshold)
        {
            return list is not null && list.Any(x => FlowPredicates.FuzzyContains(v, x, threshold));
        }

        internal static bool StringLevenshteinAtMostAnyInList(string? v, IEnumerable<string>? list, int distance)
        {
            return v is not null && list is not null && list.Any(x => x is not null && v.LevenshteinDistance(x) <= distance);
        }

        internal static bool StringSimilarityAtLeastAnyInList(string? v, IEnumerable<string>? list, double threshold)
        {
            return v is not null && list is not null && list.Any(x => x is not null && v.SimilarityTo(x) >= threshold);
        }

        internal static bool StringJaroWinklerAtLeastAnyInList(string? v, IEnumerable<string>? list, double threshold)
        {
            return v is not null && list is not null && list.Any(x => x is not null && v.JaroWinklerSimilarity(x) >= threshold);
        }

        internal static bool StringSomehowInList(string? v, IEnumerable<string> list)
        {
            return v.IsSomehowInList(list);
        }

        internal static bool StringSomehowInListWith(string? v, IEnumerable<string> list, string? options)
        {
            return v.IsSomehowInList(list, options);
        }

        internal static bool StringSomehowLike(string? v, string? other)
        {
            return v.IsSomehowLike(other);
        }

        internal static bool StringSomehowLikeWith(string? v, string? other, string? options)
        {
            return v.IsSomehowLike(other, options);
        }

        internal static bool StringContainsSomehowInList(string? v, IEnumerable<string> list)
        {
            return v.ContainsSomehowInList(list);
        }

        internal static bool StringContainsSomehowInListWith(string? v, IEnumerable<string> list, string? options)
        {
            return v.ContainsSomehowInList(list, options);
        }

        internal static bool StringNormalisedInList(string? v, IEnumerable<string> list)
        {
            return v.IsNormalisedInList(list);
        }

        internal static bool StringLevenshteinInList(string? v, IEnumerable<string> list, int maxDistance)
        {
            return v.IsLevenshteinInList(list, maxDistance);
        }

        internal static bool StringDamerauInList(string? v, IEnumerable<string> list, int maxDistance)
        {
            return v.IsDamerauInList(list, maxDistance);
        }

        internal static bool StringSimilarInList(string? v, IEnumerable<string> list, double threshold)
        {
            return v.IsSimilarInList(list, threshold);
        }

        internal static bool StringJaroWinklerInList(string? v, IEnumerable<string> list, double threshold)
        {
            return v.IsJaroWinklerInList(list, threshold);
        }

        internal static bool StringDiceInList(string? v, IEnumerable<string> list, double threshold)
        {
            return v.IsDiceInList(list, threshold);
        }

        internal static bool StringSoundexInList(string? v, IEnumerable<string> list)
        {
            return v.IsSoundexInList(list);
        }

        internal static bool StringTokenSortInList(string? v, IEnumerable<string> list, double threshold)
        {
            return v.IsTokenSortInList(list, threshold);
        }

        internal static bool StringTokenSetInList(string? v, IEnumerable<string> list, double threshold)
        {
            return v.IsTokenSetInList(list, threshold);
        }

        internal static bool StringInitialsInList(string? v, IEnumerable<string> list)
        {
            return v.IsInitialsInList(list);
        }

        internal static bool StringFuzzyScoreAtLeast(string? v, IEnumerable<string> list, double threshold, string? options)
        {
            return v is not null && v.FuzzyBestScore(list, options) >= threshold;
        }

        internal static bool StringFuzzyMatchCountAtLeast(string? v, IEnumerable<string> list, int count, string? options)
        {
            return v is not null && v.FuzzyMatchCount(list, options) >= count;
        }
    }
}

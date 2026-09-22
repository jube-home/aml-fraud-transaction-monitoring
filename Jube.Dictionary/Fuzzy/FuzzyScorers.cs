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

using Jube.Dictionary.Extensions;

namespace Jube.Dictionary.Fuzzy
{
    public static class FuzzyScorers
    {
        public static double Exact(string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal) ? 1d : 0d;
        }

        public static int LevenshteinDistance(string a, string b)
        {
            return a.LevenshteinDistance(b);
        }

        public static int DamerauDistance(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (var i = 0; i <= a.Length; i++)
            {
                d[i, 0] = i;
            }

            for (var j = 0; j <= b.Length; j++)
            {
                d[0, j] = j;
            }

            for (var i = 1; i <= a.Length; i++)
            {
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                    }
                }
            }

            return d[a.Length, b.Length];
        }

        public static double DistanceScore(int distance, string a, string b)
        {
            var longest = Math.Max(a.Length, b.Length);
            return longest == 0 ? 1d : Math.Max(0d, 1d - (double)distance / longest);
        }

        public static double Similarity(string a, string b)
        {
            return DistanceScore(LevenshteinDistance(a, b), a, b);
        }

        public static double JaroWinkler(string a, string b)
        {
            return a.JaroWinklerSimilarity(b);
        }

        public static double Dice(string a, string b)
        {
            if (a.Length == 0 && b.Length == 0)
            {
                return 1d;
            }

            if (a.Length < 2 || b.Length < 2)
            {
                return Exact(a, b);
            }

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < a.Length - 1; i++)
            {
                var gram = a.Substring(i, 2);
                counts[gram] = counts.GetValueOrDefault(gram) + 1;
            }

            var intersection = 0;
            for (var i = 0; i < b.Length - 1; i++)
            {
                var gram = b.Substring(i, 2);
                if (counts.TryGetValue(gram, out var remaining) && remaining > 0)
                {
                    counts[gram] = remaining - 1;
                    intersection++;
                }
            }

            return 2d * intersection / (a.Length - 1 + b.Length - 1);
        }

        public static double Soundex(string a, string b)
        {
            var left = FuzzyNormaliser.Tokens(a);
            var right = FuzzyNormaliser.Tokens(b);
            if (left.Length == 0 || left.Length != right.Length)
            {
                return 0d;
            }

            for (var i = 0; i < left.Length; i++)
            {
                var leftCode = left[i].Soundex();
                var rightCode = right[i].Soundex();
                if (leftCode.Length == 0 && rightCode.Length == 0)
                {
                    if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                    {
                        return 0d;
                    }
                }
                else if (!string.Equals(leftCode, rightCode, StringComparison.Ordinal))
                {
                    return 0d;
                }
            }

            return 1d;
        }

        public static double TokenSort(string a, string b)
        {
            var left = FuzzyNormaliser.Tokens(a).OrderBy(x => x, StringComparer.Ordinal);
            var right = FuzzyNormaliser.Tokens(b).OrderBy(x => x, StringComparer.Ordinal);
            return JaroWinkler(string.Join(' ', left), string.Join(' ', right));
        }

        public static double TokenSet(string a, string b)
        {
            var left = FuzzyNormaliser.Tokens(a);
            var right = FuzzyNormaliser.Tokens(b);
            if (left.Length == 0 || right.Length == 0)
            {
                return left.Length == right.Length ? 1d : 0d;
            }

            var shorter = left.Length <= right.Length ? left : right;
            var longer = left.Length <= right.Length ? right : left;
            var used = new bool[longer.Length];
            var total = 0d;
            foreach (var token in shorter)
            {
                var best = 0d;
                var bestIndex = -1;
                for (var i = 0; i < longer.Length; i++)
                {
                    if (used[i])
                    {
                        continue;
                    }

                    var score = JaroWinkler(token, longer[i]);
                    if (score > best)
                    {
                        best = score;
                        bestIndex = i;
                    }
                }

                if (bestIndex >= 0)
                {
                    used[bestIndex] = true;
                    total += best;
                }
            }

            return total / longer.Length;
        }

        public static double Initials(string a, string b)
        {
            var left = FuzzyNormaliser.Tokens(a);
            var right = FuzzyNormaliser.Tokens(b);
            if (left.Length == 0 || left.Length != right.Length)
            {
                return 0d;
            }

            var fullMatches = 0;
            for (var i = 0; i < left.Length; i++)
            {
                if (string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    if (left[i].Length > 1)
                    {
                        fullMatches++;
                    }

                    continue;
                }

                var initialAgainstFull = left[i].Length == 1 && right[i].Length > 1 && right[i][0] == left[i][0] ||
                                         right[i].Length == 1 && left[i].Length > 1 && left[i][0] == right[i][0];
                if (!initialAgainstFull)
                {
                    return 0d;
                }
            }

            return fullMatches > 0 ? 1d : 0d;
        }
    }
}

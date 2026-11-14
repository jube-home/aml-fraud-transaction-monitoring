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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Fastenshtein;
using Jube.Dictionary.Extensions;
using Jube.Dictionary.Fuzzy;
using Jube.Engine.Sanctions.Models;

namespace Jube.Engine.Sanctions
{
    public sealed class LevenshteinDistance(double? maxDistanceRatio = null, double? maxCoverageRatio = null,
        SanctionMatchOptions options = null)
    {
        public const double DefaultMaxDistanceRatio = 0.3;
        public const double DefaultMaxCoverageRatio = 2.0;

        public List<SanctionEntryReturn> CheckMultipartString(
            string multiPartString,
            int maxDistance,
            ConcurrentDictionary<int, SanctionEntry> sanctionsEntries,
            ConcurrentDictionary<string, byte> stopTokens)
        {
            var sanctionsEntriesReturn = new ConcurrentDictionary<int, SanctionEntryReturn>();

            if (string.IsNullOrWhiteSpace(multiPartString) || sanctionsEntries.Count == 0)
            {
                return [];
            }

            var inputTokens = Tokenize(effectiveOptions.Transliterate ? multiPartString.TransliterateToLatin() : multiPartString,
                stopTokens);

            if (inputTokens.Length == 0)
            {
                return [];
            }

            Parallel.ForEach(sanctionsEntries.Values, entry =>
            {
                var sourceTokens = effectiveOptions.Transliterate ? entry.LatinTokens : entry.Tokens;

                var entryTokens = stopTokens.IsEmpty
                    ? sourceTokens
                    : sourceTokens.Where(t => !stopTokens.ContainsKey(t)).ToArray();

                if (entryTokens.Length == 0)
                {
                    return;
                }

                var result = ScoreMatch(inputTokens, entryTokens, maxDistance);

                if (result.HasValue)
                {
                    sanctionsEntriesReturn.TryAdd(entry.SanctionEntryId,
                        new SanctionEntryReturn
                        {
                            SanctionEntry = entry,
                            LevenshteinDistance = result.Value
                        });
                }
            });

            return [.. sanctionsEntriesReturn.Values.OrderBy(r => r.SanctionEntry.SanctionEntryId)];
        }

        public static double? ParseNullableDistanceRatio(string rawValue)
        {
            return !string.IsNullOrWhiteSpace(rawValue)
                   && double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Math.Clamp(value, 0, 1)
                : null;
        }

        public static double? ParseNullableCoverageRatio(string rawValue)
        {
            return !string.IsNullOrWhiteSpace(rawValue)
                   && double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? Math.Max(0, value)
                : null;
        }

        private readonly SanctionMatchOptions effectiveOptions = options ?? new SanctionMatchOptions();
        private readonly double effectiveMaxCoverageRatio = maxCoverageRatio ?? DefaultMaxCoverageRatio;
        private readonly double effectiveMaxDistanceRatio = maxDistanceRatio ?? DefaultMaxDistanceRatio;

        private int? ScoreMatch(
            string[] inputTokens,
            string[] entryTokens,
            int maxDistance)
        {
            return effectiveOptions.AlgorithmId == SanctionMatchOptions.TokenSet
                ? ScoreWholeName(inputTokens, entryTokens, maxDistance)
                : ScorePairs(inputTokens, entryTokens, maxDistance);
        }

        private int? ScoreWholeName(string[] inputTokens, string[] entryTokens, int maxDistance)
        {
            var input = string.Join(' ', inputTokens);
            var entry = string.Join(' ', entryTokens);

            if (FuzzyScorers.TokenSet(input, entry) < effectiveOptions.MinimumSimilarity)
            {
                return null;
            }

            var sortedInput = string.Join(' ', inputTokens.OrderBy(t => t, StringComparer.Ordinal));
            var sortedEntry = string.Join(' ', entryTokens.OrderBy(t => t, StringComparer.Ordinal));
            var distance = new Levenshtein(sortedEntry).DistanceFrom(sortedInput);

            return distance <= maxDistance * Math.Max(inputTokens.Length, entryTokens.Length) ? distance : null;
        }

        private int? ScorePairs(
            string[] inputTokens,
            string[] entryTokens,
            int maxDistance)
        {
            var coverageRatio = (double)inputTokens.Length / entryTokens.Length;

            if (coverageRatio < 0.5)
            {
                return null;
            }

            if (coverageRatio > effectiveMaxCoverageRatio)
            {
                return null;
            }

            var pairsToMatch = Math.Min(inputTokens.Length, entryTokens.Length);

            var usedEntryIndices = new HashSet<int>();
            var worstDistance = 0;

            foreach (var inputToken in inputTokens.OrderByDescending(t => t.Length).Take(pairsToMatch))
            {
                var bestDistance = int.MaxValue;
                var bestSimilarity = -1d;
                var bestIndex = -1;

                for (var i = 0; i < entryTokens.Length; i++)
                {
                    if (usedEntryIndices.Contains(i))
                    {
                        continue;
                    }

                    var distance = new Levenshtein(entryTokens[i]).DistanceFrom(inputToken);

                    if (effectiveOptions.AlgorithmId == SanctionMatchOptions.JaroWinkler)
                    {
                        var similarity = FuzzyScorers.JaroWinkler(inputToken, entryTokens[i]);

                        if (similarity > bestSimilarity || (similarity == bestSimilarity && distance < bestDistance))
                        {
                            bestSimilarity = similarity;
                            bestDistance = distance;
                            bestIndex = i;
                        }

                        continue;
                    }

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                if (bestIndex == -1)
                {
                    return null;
                }

                if (effectiveOptions.AlgorithmId == SanctionMatchOptions.JaroWinkler)
                {
                    if (bestSimilarity < effectiveOptions.MinimumSimilarity || bestDistance > maxDistance)
                    {
                        return null;
                    }
                }
                else
                {
                    var tokenThreshold = EffectiveTokenThreshold(
                        inputToken.Length, entryTokens[bestIndex].Length, maxDistance);

                    if (bestDistance > tokenThreshold)
                    {
                        return null;
                    }
                }

                usedEntryIndices.Add(bestIndex);
                worstDistance = Math.Max(worstDistance, bestDistance);
            }

            return worstDistance;
        }

        private int EffectiveTokenThreshold(
            int inputTokenLength, int entryTokenLength, int maxDistance)
        {
            var shorterLength = Math.Min(inputTokenLength, entryTokenLength);
            var ratioBound = Math.Max(1, (int)Math.Floor(shorterLength * effectiveMaxDistanceRatio));

            return Math.Min(maxDistance, ratioBound);
        }

        private static string[] Tokenize(string raw, ConcurrentDictionary<string, byte> stopTokens)
        {
            return
            [
                .. SanctionTextFolding.Tokens(raw)
                    .Where(t => !stopTokens.ContainsKey(t))
            ];
        }
    }
}
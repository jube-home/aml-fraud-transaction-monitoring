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

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Jube.Dictionary.Fuzzy
{
    public readonly record struct FuzzyResult(int Count, double BestScore, string? BestEntry)
    {
        public static readonly FuzzyResult None = new(0, 0d, null);
    }

    public static class FuzzyListMatcher
    {
        private sealed record Entry(string Original, string Normalised, string[] Tokens);

        private sealed class Snapshot(int count, int fingerprint, Entry[] items)
        {
            public int Count { get; } = count;
            public int Fingerprint { get; } = fingerprint;
            public Entry[] Items { get; } = items;
        }

        private static readonly ConditionalWeakTable<object, ConcurrentDictionary<string, Snapshot>> Snapshots = new();

        public static bool Any(string? value, IEnumerable<string>? list, FuzzyOptions options)
        {
            return Evaluate(value, list, options, true).Count > 0;
        }

        public static FuzzyResult Evaluate(string? value, IEnumerable<string>? list, FuzzyOptions options,
            bool stopAtFirst = false)
        {
            if (value is null || list is null || !options.IsValid)
            {
                return FuzzyResult.None;
            }

            var normalisedValue = FuzzyNormaliser.Normalise(value, options.Normalisation);
            if (normalisedValue.Length == 0 || normalisedValue.Length < options.MinLength)
            {
                return FuzzyResult.None;
            }

            var valueTokens = FuzzyNormaliser.Tokens(normalisedValue);
            var count = 0;
            var bestScore = 0d;
            string? bestEntry = null;

            foreach (var entry in Entries(list, options.Normalisation))
            {
                if (entry.Normalised.Length == 0)
                {
                    continue;
                }

                if (options is { FirstLetter: true, Within: false } && entry.Normalised[0] != normalisedValue[0])
                {
                    continue;
                }

                var (passed, score) = options.Within
                    ? MatchWithin(normalisedValue, valueTokens, entry, options)
                    : MatchAlgorithms(normalisedValue, entry.Normalised, options);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestEntry = entry.Original;
                }

                if (!passed)
                {
                    continue;
                }

                count++;
                if (stopAtFirst)
                {
                    return new FuzzyResult(count, bestScore, bestEntry);
                }
            }

            return new FuzzyResult(count, bestScore, bestEntry);
        }

        public static (bool Passed, double Score) MatchAlgorithms(string a, string b, FuzzyOptions options)
        {
            var passedAll = true;
            var passedAny = false;
            var maximum = 0d;
            var minimum = 1d;

            foreach (var spec in options.Algorithms)
            {
                var (passed, score) = Score(spec, a, b);
                passedAll &= passed;
                passedAny |= passed;
                maximum = Math.Max(maximum, score);
                minimum = Math.Min(minimum, score);
            }

            return options.RequireAll ? (passedAll, minimum) : (passedAny, maximum);
        }

        public static (bool Passed, double Score) Score(FuzzyAlgorithmSpec spec, string a, string b)
        {
            switch (spec.Algorithm)
            {
                case FuzzyAlgorithm.Levenshtein:
                {
                    var distance = FuzzyScorers.LevenshteinDistance(a, b);
                    return (distance <= spec.Threshold, FuzzyScorers.DistanceScore(distance, a, b));
                }
                case FuzzyAlgorithm.Damerau:
                {
                    var distance = FuzzyScorers.DamerauDistance(a, b);
                    return (distance <= spec.Threshold, FuzzyScorers.DistanceScore(distance, a, b));
                }
                case FuzzyAlgorithm.Similarity:
                    return Threshold(FuzzyScorers.Similarity(a, b), spec.Threshold);
                case FuzzyAlgorithm.JaroWinkler:
                    return Threshold(FuzzyScorers.JaroWinkler(a, b), spec.Threshold);
                case FuzzyAlgorithm.Dice:
                    return Threshold(FuzzyScorers.Dice(a, b), spec.Threshold);
                case FuzzyAlgorithm.TokenSort:
                    return Threshold(FuzzyScorers.TokenSort(a, b), spec.Threshold);
                case FuzzyAlgorithm.TokenSet:
                    return Threshold(FuzzyScorers.TokenSet(a, b), spec.Threshold);
                case FuzzyAlgorithm.Soundex:
                    return Threshold(FuzzyScorers.Soundex(a, b), 1d);
                case FuzzyAlgorithm.Initials:
                    return Threshold(FuzzyScorers.Initials(a, b), 1d);
                default:
                    return Threshold(FuzzyScorers.Exact(a, b), 1d);
            }
        }

        private static (bool Passed, double Score) Threshold(double score, double threshold)
        {
            return (score >= threshold, score);
        }

        private static (bool Passed, double Score) MatchWithin(string value, string[] valueTokens, Entry entry,
            FuzzyOptions options)
        {
            var bestPassed = false;
            var bestScore = 0d;

            if (options.Normalisation.RemoveSpaces)
            {
                var length = entry.Normalised.Length;
                if (value.Length < length)
                {
                    return (false, 0d);
                }

                for (var i = 0; i + length <= value.Length; i++)
                {
                    var (passed, score) = MatchAlgorithms(value.Substring(i, length), entry.Normalised, options);
                    bestPassed |= passed;
                    bestScore = Math.Max(bestScore, score);
                }

                return (bestPassed, bestScore);
            }

            var window = entry.Tokens.Length;
            if (window == 0 || valueTokens.Length < window)
            {
                return (false, 0d);
            }

            for (var i = 0; i + window <= valueTokens.Length; i++)
            {
                var candidate = string.Join(' ', valueTokens, i, window);
                var (passed, score) = MatchAlgorithms(candidate, entry.Normalised, options);
                bestPassed |= passed;
                bestScore = Math.Max(bestScore, score);
            }

            return (bestPassed, bestScore);
        }

        private static IEnumerable<Entry> Entries(IEnumerable<string> list, FuzzyNormalisation normalisation)
        {
            if (list is not IList<string> indexed)
            {
                return Build(list, normalisation);
            }

            var perList = Snapshots.GetValue(list, _ => new ConcurrentDictionary<string, Snapshot>());
            var signature = normalisation.Signature;
            var count = indexed.Count;
            var fingerprint = Fingerprint(indexed);

            if (perList.TryGetValue(signature, out var snapshot) &&
                snapshot.Count == count && snapshot.Fingerprint == fingerprint)
            {
                return snapshot.Items;
            }

            var built = Build(indexed, normalisation);
            perList[signature] = new Snapshot(count, fingerprint, built);
            return built;
        }

        private static int Fingerprint(IList<string> list)
        {
            if (list.Count == 0)
            {
                return 0;
            }

            return HashCode.Combine(list[0], list[list.Count / 2], list[^1], list.Count);
        }

        private static Entry[] Build(IEnumerable<string> list, FuzzyNormalisation normalisation)
        {
            var items = new List<Entry>();
            foreach (var original in list)
            {
                var normalised = FuzzyNormaliser.Normalise(original, normalisation);
                items.Add(new Entry(original, normalised, FuzzyNormaliser.Tokens(normalised)));
            }

            return items.ToArray();
        }
    }
}

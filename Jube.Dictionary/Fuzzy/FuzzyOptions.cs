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
using System.Globalization;

namespace Jube.Dictionary.Fuzzy
{
    public sealed class FuzzyOptions
    {
        private const int MaximumDistance = 10;
        private const int CacheLimit = 1024;

        private static readonly ConcurrentDictionary<string, FuzzyOptions> Cache = new(StringComparer.Ordinal);

        private static readonly FuzzyAlgorithmSpec[] DefaultAlgorithms =
        [
            new(FuzzyAlgorithm.JaroWinkler, 0.92),
            new(FuzzyAlgorithm.TokenSort, 0.92)
        ];

        public static readonly FuzzyOptions Default = new()
        {
            Algorithms = DefaultAlgorithms,
            Normalisation = FuzzyNormalisation.Default,
            Canonical = "jarowinkler=0.92,tokensort=0.92"
        };

        public string? Error { get; init; }

        public IReadOnlyList<FuzzyAlgorithmSpec> Algorithms { get; init; } = DefaultAlgorithms;

        public bool RequireAll { get; init; }

        public FuzzyNormalisation Normalisation { get; init; } = FuzzyNormalisation.Default;

        public bool Within { get; init; }

        public int MinLength { get; init; }

        public bool FirstLetter { get; init; }

        public string Canonical { get; init; } = "";

        public bool IsValid => Error is null;

        public static double DefaultThreshold(FuzzyAlgorithm algorithm)
        {
            return algorithm switch
            {
                FuzzyAlgorithm.Levenshtein => 1,
                FuzzyAlgorithm.Damerau => 1,
                FuzzyAlgorithm.Similarity => 0.85,
                FuzzyAlgorithm.JaroWinkler => 0.92,
                FuzzyAlgorithm.Dice => 0.85,
                FuzzyAlgorithm.TokenSort => 0.92,
                FuzzyAlgorithm.TokenSet => 0.85,
                _ => 1
            };
        }

        public static FuzzyOptions For(FuzzyAlgorithm algorithm, double threshold, bool within = false)
        {
            var valid = IsValidThreshold(algorithm, threshold);
            return new FuzzyOptions
            {
                Algorithms = [new FuzzyAlgorithmSpec(algorithm, threshold)],
                Within = within,
                Error = valid ? null : ThresholdError(algorithm, threshold),
                Canonical = algorithm + "=" + threshold.ToString("R", CultureInfo.InvariantCulture)
            };
        }

        public static FuzzyOptions Parse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Default;
            }

            if (Cache.TryGetValue(text, out var cached))
            {
                return cached;
            }

            var parsed = ParseUncached(text);
            if (Cache.Count >= CacheLimit)
            {
                Cache.Clear();
            }

            Cache[text] = parsed;

            return parsed;
        }

        private static FuzzyOptions Failed(string message)
        {
            return new FuzzyOptions { Error = message, Canonical = "invalid" };
        }

        private static bool IsValidThreshold(FuzzyAlgorithm algorithm, double threshold)
        {
            if (double.IsNaN(threshold))
            {
                return false;
            }

            return algorithm is FuzzyAlgorithm.Levenshtein or FuzzyAlgorithm.Damerau
                ? threshold is >= 0 and <= MaximumDistance && Math.Abs(threshold - Math.Floor(threshold)) < 0.0001
                : threshold is > 0 and <= 1;
        }

        private static string ThresholdError(FuzzyAlgorithm algorithm, double threshold)
        {
            return algorithm is FuzzyAlgorithm.Levenshtein or FuzzyAlgorithm.Damerau
                ? $"{algorithm} takes a whole number of edits from 0 to {MaximumDistance}, not '{threshold}'."
                : $"{algorithm} takes a threshold above 0 and up to 1, not '{threshold}'.";
        }

        private static FuzzyOptions ParseUncached(string text)
        {
            var algorithms = new List<FuzzyAlgorithmSpec>();
            var ignoreCase = true;
            var removeDiacritics = true;
            var stripPunctuation = true;
            var removeSpaces = false;
            var removeTitles = false;
            var removeSuffixes = false;
            var within = false;
            var requireAll = false;
            var minLength = 0;
            var firstLetter = false;

            foreach (var raw in text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries |
                                                          StringSplitOptions.TrimEntries))
            {
                var equals = raw.IndexOf('=');
                var key = (equals < 0 ? raw : raw[..equals]).Trim().ToLowerInvariant();
                var value = equals < 0 ? null : raw[(equals + 1)..].Trim();

                if (TryAlgorithm(key, out var algorithm))
                {
                    var threshold = DefaultThreshold(algorithm);
                    if (value is not null &&
                        !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out threshold))
                    {
                        return Failed($"'{value}' is not a number for {key}.");
                    }

                    if (algorithm is FuzzyAlgorithm.Exact or FuzzyAlgorithm.Soundex or FuzzyAlgorithm.Initials)
                    {
                        if (value is not null)
                        {
                            return Failed($"{key} takes no value.");
                        }

                        threshold = 1;
                    }
                    else if (!IsValidThreshold(algorithm, threshold))
                    {
                        return Failed(ThresholdError(algorithm, threshold));
                    }

                    algorithms.Add(new FuzzyAlgorithmSpec(algorithm, threshold));
                    continue;
                }

                if (value is not null && key != "minlength")
                {
                    return Failed($"The option '{key}' takes no value.");
                }

                switch (key)
                {
                    case "case":
                        ignoreCase = false;
                        break;
                    case "nocase":
                        ignoreCase = true;
                        break;
                    case "diacritics":
                        removeDiacritics = false;
                        break;
                    case "nodiacritics":
                        removeDiacritics = true;
                        break;
                    case "punct":
                        stripPunctuation = false;
                        break;
                    case "nopunct":
                        stripPunctuation = true;
                        break;
                    case "nospaces":
                        removeSpaces = true;
                        break;
                    case "keepspaces":
                        removeSpaces = false;
                        break;
                    case "notitles":
                        removeTitles = true;
                        break;
                    case "nosuffixes":
                        removeSuffixes = true;
                        break;
                    case "whole":
                        within = false;
                        break;
                    case "within":
                        within = true;
                        break;
                    case "all":
                        requireAll = true;
                        break;
                    case "any":
                        requireAll = false;
                        break;
                    case "firstletter":
                        firstLetter = true;
                        break;
                    case "minlength":
                        if (value is null || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                out minLength) || minLength < 0)
                        {
                            return Failed("minlength takes a whole number of characters, zero or more.");
                        }

                        break;
                    default:
                        return Failed($"Unknown fuzzy option '{key}'.");
                }
            }

            if (algorithms.Count == 0)
            {
                algorithms.AddRange(DefaultAlgorithms);
            }

            var normalisation = new FuzzyNormalisation(ignoreCase, removeDiacritics, stripPunctuation, removeSpaces,
                removeTitles, removeSuffixes);

            return new FuzzyOptions
            {
                Algorithms = algorithms,
                RequireAll = requireAll,
                Normalisation = normalisation,
                Within = within,
                MinLength = minLength,
                FirstLetter = firstLetter,
                Canonical = text.Trim().ToLowerInvariant()
            };
        }

        private static bool TryAlgorithm(string key, out FuzzyAlgorithm algorithm)
        {
            switch (key)
            {
                case "exact":
                    algorithm = FuzzyAlgorithm.Exact;
                    return true;
                case "levenshtein":
                case "lev":
                    algorithm = FuzzyAlgorithm.Levenshtein;
                    return true;
                case "damerau":
                case "osa":
                    algorithm = FuzzyAlgorithm.Damerau;
                    return true;
                case "similarity":
                case "sim":
                    algorithm = FuzzyAlgorithm.Similarity;
                    return true;
                case "jarowinkler":
                case "jw":
                    algorithm = FuzzyAlgorithm.JaroWinkler;
                    return true;
                case "dice":
                case "bigram":
                    algorithm = FuzzyAlgorithm.Dice;
                    return true;
                case "soundex":
                    algorithm = FuzzyAlgorithm.Soundex;
                    return true;
                case "tokensort":
                case "sort":
                    algorithm = FuzzyAlgorithm.TokenSort;
                    return true;
                case "tokenset":
                case "set":
                    algorithm = FuzzyAlgorithm.TokenSet;
                    return true;
                case "initials":
                    algorithm = FuzzyAlgorithm.Initials;
                    return true;
                default:
                    algorithm = default;
                    return false;
            }
        }
    }
}

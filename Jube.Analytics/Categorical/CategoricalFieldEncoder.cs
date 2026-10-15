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

using System.Text.Json;
using Jube.Analytics.Categorical.Models;

namespace Jube.Analytics.Categorical
{
    public sealed class CategoricalFieldEncoder
    {
        public const string OtherToken = "[Other]";
        public const string MissingToken = "[Missing]";
        private const int MaxStateLevels = 10000;

        private static readonly JsonSerializerOptions serialiser = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Dictionary<string, int> indicatorByKey;
        private readonly int missingIndex;
        private readonly int otherIndex;
        private readonly HashSet<string> referenceKeys;

        private CategoricalFieldEncoder(string sourceName, IReadOnlyList<CategoricalLevel> levels, bool hasOther,
            long pooledCount, bool hasMissing, long missingCount, long totalCount)
        {
            SourceName = sourceName;
            Levels = levels;
            HasOther = hasOther;
            PooledCount = pooledCount;
            HasMissing = hasMissing;
            MissingCount = missingCount;
            TotalCount = totalCount;

            var indicators = new List<CategoricalIndicator>();
            indicatorByKey = new Dictionary<string, int>(StringComparer.Ordinal);
            referenceKeys = [levels[0].Key];

            for (var i = 1; i < levels.Count; i++)
            {
                indicatorByKey[levels[i].Key] = indicators.Count;

                indicators.Add(new CategoricalIndicator(CategoricalIndicatorKind.Level, levels[i].Key, levels[i].Label,
                    $"{sourceName}={levels[i].Label}", levels[i].Count));
            }

            otherIndex = -1;

            if (hasOther)
            {
                otherIndex = indicators.Count;

                indicators.Add(new CategoricalIndicator(CategoricalIndicatorKind.Other, OtherToken, OtherToken,
                    $"{sourceName}={OtherToken}", pooledCount));
            }

            missingIndex = -1;

            if (hasMissing)
            {
                missingIndex = indicators.Count;

                indicators.Add(new CategoricalIndicator(CategoricalIndicatorKind.Missing, MissingToken, MissingToken,
                    $"{sourceName}={MissingToken}", missingCount));
            }

            Indicators = indicators;
        }

        public string SourceName { get; }
        public IReadOnlyList<CategoricalLevel> Levels { get; }
        public IReadOnlyList<CategoricalIndicator> Indicators { get; }
        public bool HasOther { get; }
        public bool HasMissing { get; }
        public long PooledCount { get; }
        public long MissingCount { get; }
        public long TotalCount { get; }
        public CategoricalLevel Reference => Levels[0];

        public static CategoricalFieldEncoder? Fit(string sourceName, CategoricalCounter counter,
            CategoricalOptions options, int? maxLevelsOverride = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
            ArgumentNullException.ThrowIfNull(counter);
            ArgumentNullException.ThrowIfNull(options);

            var nonMissing = counter.NonMissing;

            if (nonMissing == 0 || counter.KeyCounts.Count == 0)
            {
                return null;
            }

            var minimum = options.EffectiveMinimum(nonMissing);
            var maxLevels = Math.Max(1, maxLevelsOverride ?? options.MaxLevels);

            var ranked = counter.KeyCounts
                .OrderByDescending(o => o.Value)
                .ThenBy(o => o.Key, StringComparer.Ordinal)
                .ToList();

            var kept = new List<CategoricalLevel>();
            long covered = 0;

            foreach (var (key, count) in ranked)
            {
                if (kept.Count == 0)
                {
                    kept.Add(new CategoricalLevel(key, counter.LabelFor(key), count));
                    covered += count;
                    continue;
                }

                if (kept.Count >= maxLevels || count < minimum
                                            || covered >= options.CoverageTarget * nonMissing)
                {
                    break;
                }

                kept.Add(new CategoricalLevel(key, counter.LabelFor(key), count));
                covered += count;
            }

            var pooled = nonMissing - covered;
            var hasOther = pooled >= minimum;
            var hasMissing = counter.Missing >= minimum;

            if (kept.Count == 1 && !hasOther && !hasMissing)
            {
                return null;
            }

            return new CategoricalFieldEncoder(sourceName, kept, hasOther, pooled, hasMissing, counter.Missing,
                counter.Total);
        }

        public int Resolve(string? raw)
        {
            var key = CategoricalNormaliser.Key(raw);

            if (key == null)
            {
                return missingIndex;
            }

            if (referenceKeys.Contains(key))
            {
                return -1;
            }

            return indicatorByKey.GetValueOrDefault(key, otherIndex);
        }

        public double Indicator(string? raw, int indicatorIndex)
        {
            return Resolve(raw) == indicatorIndex ? 1d : 0d;
        }

        public void Encode(string? raw, Span<double> destination)
        {
            if (destination.Length < Indicators.Count)
            {
                throw new ArgumentException("The destination is shorter than the indicator count.",
                    nameof(destination));
            }

            destination[..Indicators.Count].Clear();
            var index = Resolve(raw);

            if (index >= 0)
            {
                destination[index] = 1d;
            }
        }

        public CategoricalFieldState ToState()
        {
            return new CategoricalFieldState
            {
                SourceName = SourceName,
                Levels = Levels
                    .Select(s => (CategoricalLevelState?)new CategoricalLevelState
                        { Key = s.Key, Label = s.Label, Count = s.Count })
                    .ToList(),
                HasOther = HasOther,
                PooledCount = PooledCount,
                HasMissing = HasMissing,
                MissingCount = MissingCount,
                TotalCount = TotalCount
            };
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(ToState(), serialiser);
        }

        public static CategoricalFieldEncoder FromJson(string json)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            CategoricalFieldState? state;

            try
            {
                state = JsonSerializer.Deserialize<CategoricalFieldState>(json, serialiser);
            }
            catch (JsonException ex)
            {
                throw new FormatException("The categorical vocabulary is not valid JSON.", ex);
            }

            return FromState(state ?? throw new FormatException("The categorical vocabulary is empty."));
        }

        public static CategoricalFieldEncoder FromState(CategoricalFieldState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (state.Version != 1)
            {
                throw new NotSupportedException($"Categorical vocabulary version {state.Version} is not supported.");
            }

            if (state.NormalisationVersion != CategoricalNormaliser.Version)
            {
                throw new NotSupportedException(
                    $"Categorical normalisation version {state.NormalisationVersion} is not supported.");
            }

            if (string.IsNullOrWhiteSpace(state.SourceName))
            {
                throw new FormatException("The categorical vocabulary has no source name.");
            }

            if (state.Levels == null || state.Levels.Count == 0 || state.Levels.Count > MaxStateLevels)
            {
                throw new FormatException("The categorical vocabulary has an invalid number of levels.");
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var levels = new List<CategoricalLevel>(state.Levels.Count);

            foreach (var level in state.Levels)
            {
                if (level == null || string.IsNullOrEmpty(level.Key) ||
                    CategoricalNormaliser.Key(level.Key) != level.Key
                    || !seen.Add(level.Key) || level.Count < 0)
                {
                    throw new FormatException("The categorical vocabulary has an invalid level.");
                }

                levels.Add(new CategoricalLevel(level.Key,
                    string.IsNullOrEmpty(level.Label) ? level.Key : CategoricalNormaliser.Label(level.Label),
                    level.Count));
            }

            if (state.PooledCount < 0 || state.MissingCount < 0 || state.TotalCount < 0)
            {
                throw new FormatException("The categorical vocabulary has an invalid count.");
            }

            return new CategoricalFieldEncoder(state.SourceName, levels, state.HasOther, state.PooledCount,
                state.HasMissing, state.MissingCount, state.TotalCount);
        }
    }
}
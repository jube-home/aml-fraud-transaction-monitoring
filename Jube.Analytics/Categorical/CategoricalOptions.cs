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

namespace Jube.Analytics.Categorical
{
    public sealed class CategoricalOptions
    {
        private static readonly JsonSerializerOptions serialiser = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public bool Enabled { get; set; }
        public int MaxLevels { get; set; } = 25;
        public int MinLevelCount { get; set; } = 20;
        public double MinLevelFraction { get; set; }
        public double CoverageTarget { get; set; } = 1d;
        public int MaxTotalIndicators { get; set; } = 100;
        public int MaxDistinctTracked { get; set; } = 50000;
        public double NumericLookingThreshold { get; set; } = 0.8d;
        public double IdentifierRatio { get; set; } = 0.9d;
        public List<string> Exclude { get; set; } = [];
        public List<string> Include { get; set; } = [];

        public static CategoricalOptions FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new CategoricalOptions();
            }

            CategoricalOptions? parsed;

            try
            {
                parsed = JsonSerializer.Deserialize<CategoricalOptions>(json, serialiser);
            }
            catch (JsonException)
            {
                return new CategoricalOptions();
            }

            return parsed?.Sanitised() ?? new CategoricalOptions();
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, serialiser);
        }

        public CategoricalOptions Sanitised()
        {
            MaxLevels = Math.Clamp(MaxLevels, 1, 1000);
            MinLevelCount = Math.Clamp(MinLevelCount, 1, int.MaxValue);
            MinLevelFraction = double.IsFinite(MinLevelFraction) ? Math.Clamp(MinLevelFraction, 0d, 1d) : 0d;
            CoverageTarget = double.IsFinite(CoverageTarget) ? Math.Clamp(CoverageTarget, 0.01d, 1d) : 1d;
            MaxTotalIndicators = Math.Clamp(MaxTotalIndicators, 1, 5000);
            MaxDistinctTracked = Math.Clamp(MaxDistinctTracked, 10, 1_000_000);

            NumericLookingThreshold = double.IsFinite(NumericLookingThreshold)
                ? Math.Clamp(NumericLookingThreshold, 0.5d, 1d)
                : 0.8d;
            IdentifierRatio = double.IsFinite(IdentifierRatio) ? Math.Clamp(IdentifierRatio, 0.1d, 1d) : 0.9d;

            // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            Exclude = Exclude?.Where(w => !string.IsNullOrWhiteSpace(w)).Select(s => s.Trim()).Distinct().ToList() ??
                      [];
            Include = Include?.Where(w => !string.IsNullOrWhiteSpace(w)).Select(s => s.Trim()).Distinct().ToList() ??
                      [];

            // ReSharper restore ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            return this;
        }

        internal long EffectiveMinimum(long nonMissing)
        {
            var fraction = (long)Math.Ceiling(MinLevelFraction * nonMissing);

            return Math.Max(1, Math.Max(MinLevelCount, fraction));
        }
    }
}
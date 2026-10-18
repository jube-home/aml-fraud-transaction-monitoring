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

using System.Globalization;

namespace Jube.Analytics.Categorical
{
    public sealed class CategoricalCounter(int maxDistinct = 50000)
    {
        private const int SpellingsPerKey = 4;
        private readonly Dictionary<string, long> keyCounts = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, long>> spellings = new(StringComparer.Ordinal);
        public long Total { get; private set; }
        public long Missing { get; private set; }
        public long Overflow { get; private set; }
        public long NumericLooking { get; private set; }
        public long NonMissing => Total - Missing;
        public int Distinct => keyCounts.Count;
        public bool DistinctTruncated => Overflow > 0;
        public IReadOnlyDictionary<string, long> KeyCounts => keyCounts;

        public void Add(string? raw)
        {
            Total++;
            var key = CategoricalNormaliser.Key(raw);

            if (key == null)
            {
                Missing++;

                return;
            }

            if (double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                && double.IsFinite(number))
            {
                NumericLooking++;
            }

            if (!keyCounts.TryGetValue(key, out var count))
            {
                if (keyCounts.Count >= maxDistinct)
                {
                    Overflow++;

                    return;
                }

                count = 0;
            }

            keyCounts[key] = count + 1;

            var label = CategoricalNormaliser.Label(raw);

            if (!spellings.TryGetValue(key, out var variants))
            {
                variants = new Dictionary<string, long>(StringComparer.Ordinal);
                spellings[key] = variants;
            }

            if (variants.TryGetValue(label, out var seen))
            {
                variants[label] = seen + 1;
            }
            else if (variants.Count < SpellingsPerKey)
            {
                variants[label] = 1;
            }
        }

        public string LabelFor(string key)
        {
            if (!spellings.TryGetValue(key, out var variants) || variants.Count == 0)
            {
                return key;
            }

            return variants.OrderByDescending(o => o.Value).ThenBy(o => o.Key, StringComparer.Ordinal).First().Key;
        }
    }
}
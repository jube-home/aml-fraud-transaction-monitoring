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

using Jube.Dto.Options;

namespace Jube.CaseRisk.Similarity
{
    public static class SimilarityVectorBuilder
    {
        public static IEnumerable<SimilarityComponent> Flags(CaseSimilarityKind kind, IEnumerable<string?> values) =>
            Distinct(values).Select(value => new SimilarityComponent(kind, value));

        public static IEnumerable<SimilarityComponent> Values(CaseSimilarityKind kind, string field,
            IEnumerable<string?> values, SimilaritySettings settings)
        {
            var distinct = Distinct(values);

            return distinct.Count > settings.MaximumValuesPerField
                ? []
                : distinct.Select(value => new SimilarityComponent(kind, field + "=" + value));
        }

        public static IEnumerable<SimilarityComponent> Measures(CaseSimilarityKind kind, string field,
            IReadOnlyCollection<double> values)
        {
            var usable = values.Where(value => !double.IsNaN(value) && !double.IsInfinity(value)).ToList();

            if (usable.Count == 0)
            {
                yield break;
            }

            foreach (var (name, value) in new[]
                     {
                         ("sum", usable.Sum()), ("mean", usable.Average()), ("max", usable.Max())
                     })
            {
                var band = SimilarityBanding.Band(value);

                if (band is not null)
                {
                    yield return new SimilarityComponent(kind, field + ":" + name + "=" + band);
                }
            }
        }

        private static List<string> Distinct(IEnumerable<string?> values) =>
        [
            .. values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!)
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)
        ];
    }
}

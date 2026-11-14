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
    public static class SimilarityCalculator
    {
        public static CaseSimilarityBand BandOf(double score, double sharedWeight, SimilaritySettings settings)
        {
            if (sharedWeight < settings.MinimumSharedWeight)
            {
                return CaseSimilarityBand.Weak;
            }

            if (score >= settings.StrongThreshold)
            {
                return CaseSimilarityBand.Strong;
            }

            return score >= settings.ModerateThreshold ? CaseSimilarityBand.Moderate : CaseSimilarityBand.Weak;
        }

        public static SimilarityResult Compare(IReadOnlyCollection<SimilarityComponent> left,
            IReadOnlyCollection<SimilarityComponent> right, Func<SimilarityComponent, double> weight,
            SimilaritySettings settings)
        {
            var leftByKind = left.Distinct().ToLookup(component => component.Kind);
            var rightByKind = right.Distinct().ToLookup(component => component.Kind);

            List<SimilarityKindScore> kinds =
            [
                .. leftByKind.Select(group => group.Key).Intersect(rightByKind.Select(group => group.Key))
                    .Select(kind => ScoreKind(kind, Weighted(leftByKind[kind], weight),
                        Weighted(rightByKind[kind], weight)))
                    .OfType<SimilarityKindScore>()
                    .OrderBy(kind => kind.Kind)
            ];

            var kindWeight = kinds.Sum(kind => settings.KindWeights.GetValueOrDefault(kind.Kind, 1));

            var score = kindWeight > 0
                ? kinds.Sum(kind => settings.KindWeights.GetValueOrDefault(kind.Kind, 1) * kind.Score) / kindWeight
                : 0;

            var allShared = kinds.Sum(kind => kind.SharedWeight);

            return new SimilarityResult(score, BandOf(score, allShared, settings), allShared, kinds);
        }

        private static SimilarityKindScore? ScoreKind(CaseSimilarityKind kind,
            Dictionary<SimilarityComponent, double> left, Dictionary<SimilarityComponent, double> right)
        {
            if (left.Count == 0 || right.Count == 0)
            {
                return null;
            }

            List<SimilarityComponent> shared =
            [
                .. left.Keys.Where(right.ContainsKey)
                    .OrderByDescending(component => left[component])
                    .ThenBy(component => component.Value, StringComparer.Ordinal)
            ];

            var sharedWeight = shared.Sum(component => left[component]);
            var totalWeight = left.Values.Sum() + right.Values.Sum() - sharedWeight;

            return new SimilarityKindScore(kind, sharedWeight / totalWeight, sharedWeight, totalWeight, shared);
        }

        private static Dictionary<SimilarityComponent, double> Weighted(IEnumerable<SimilarityComponent> components,
            Func<SimilarityComponent, double> weight) =>
            components.Select(component => (Component: component, Weight: weight(component)))
                .Where(pair => pair.Weight > 0)
                .ToDictionary(pair => pair.Component, pair => pair.Weight);
    }
}

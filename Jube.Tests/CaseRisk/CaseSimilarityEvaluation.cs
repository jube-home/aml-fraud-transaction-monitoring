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
using System.Collections.Generic;
using System.Linq;
using Jube.CaseRisk.Similarity;
using Jube.Dto.Options;

namespace Jube.Test.CaseRisk
{
    internal static class CaseSimilarityEvaluation
    {
        public static CaseSimilarityEvaluationOutcome LeaveOneOut(IReadOnlyList<CaseSimilarityEvaluationCase> cases,
            Func<CaseSimilarityEvaluationCase, CaseSimilarityEvaluationCase, bool> similar)
        {
            var predictions = new List<(double Score, bool Confirmed)>();
            var abstained = 0;

            foreach (var target in cases)
            {
                var neighbours = cases
                    .Where(other => other.Id != target.Id && other.Closed < target.Created && similar(target, other))
                    .ToList();

                if (neighbours.Count == 0)
                {
                    abstained++;

                    continue;
                }

                predictions.Add((neighbours.Count(n => n.Confirmed) / (double)neighbours.Count, target.Confirmed));
            }

            return new CaseSimilarityEvaluationOutcome(Auc(predictions), predictions.Count, abstained);
        }

        public static Func<CaseSimilarityEvaluationCase, CaseSimilarityEvaluationCase, bool> ByVector(
            IReadOnlyList<CaseSimilarityEvaluationCase> cases, SimilaritySettings settings)
        {
            var counts = cases.SelectMany(c => c.Vector.Distinct()).GroupBy(c => c)
                .ToDictionary(g => g.Key, g => g.Count());

            return (target, other) => SimilarityCalculator.Compare(target.Vector, other.Vector,
                component => SimilarityWeight.Of(counts.GetValueOrDefault(component), cases.Count, settings),
                settings).Band >= CaseSimilarityBand.Moderate;
        }

        public static Func<CaseSimilarityEvaluationCase, CaseSimilarityEvaluationCase, bool> ByShared(
            CaseSimilarityKind kind)
        {
            return (target, other) => target.Vector.Where(c => c.Kind == kind).Intersect(other.Vector).Any();
        }

        public static double Auc(IReadOnlyCollection<(double Score, bool Confirmed)> predictions)
        {
            var positives = predictions.Where(p => p.Confirmed).Select(p => p.Score).ToList();
            var negatives = predictions.Where(p => !p.Confirmed).Select(p => p.Score).ToList();

            if (positives.Count == 0 || negatives.Count == 0)
            {
                return 0.5;
            }

            var wins = positives.Sum(positive => negatives.Sum(negative =>
                positive > negative ? 1.0 : positive.Equals(negative) ? 0.5 : 0.0));

            return wins / (positives.Count * (double)negatives.Count);
        }
    }
}

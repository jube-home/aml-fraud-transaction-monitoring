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
    public sealed record SimilaritySettings
    {
        public double StrongThreshold { get; init; } = 0.6;
        public double ModerateThreshold { get; init; } = 0.35;
        public double MinimumSharedWeight { get; init; } = 2;
        public int MinimumCaseCount { get; init; } = 2;
        public int MaximumValuesPerField { get; init; } = 12;
        public int MaximumComponents { get; init; } = 400;
        public int MaximumComponentLength { get; init; } = 300;

        public IReadOnlyDictionary<CaseSimilarityKind, double> KindWeights { get; init; } =
            new Dictionary<CaseSimilarityKind, double>
            {
                [CaseSimilarityKind.Rule] = 3,
                [CaseSimilarityKind.Model] = 1,
                [CaseSimilarityKind.Tag] = 3,
                [CaseSimilarityKind.Entity] = 3,
                [CaseSimilarityKind.Category] = 1,
                [CaseSimilarityKind.Measure] = 1,
                [CaseSimilarityKind.Feature] = 1,
                [CaseSimilarityKind.Band] = 1
            };
    }
}

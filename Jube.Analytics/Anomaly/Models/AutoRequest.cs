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

using Jube.Analytics.Anomaly.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Analytics.Anomaly.Models
{
    public sealed record AutoCandidate(AnomalyAlgorithm Algorithm, string Name, Func<IAnomalyDetector> Factory);

    public sealed record AutoRequest
    {
        public required IReadOnlyList<AutoCandidate> Candidates { get; init; }
        public required double[][] Data { get; init; }
        public double[][]? LabelledPositives { get; init; }
        public double Contamination { get; init; } = 0.05;
        public double TimeBudgetSeconds { get; init; } = 120d;
        public int EvaluationRowCap { get; init; } = 2000;
        public int Resamples { get; init; } = 4;
        public int Subspaces { get; init; } = 4;
        public int Seed { get; init; }
    }

    public sealed record AutoResult(AnomalyAlgorithm? Selected, AutoEvaluation Evaluation);
}
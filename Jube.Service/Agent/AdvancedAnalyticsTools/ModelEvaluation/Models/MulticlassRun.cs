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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models
{
    internal sealed class MulticlassRun
    {
        public required MulticlassConfig Config { get; init; }
        public required Func<double[][], double[][]> Predict { get; init; }
        public required int[] TrainRows { get; init; }
        public required int[] ValidationRows { get; init; }
        public required int[] TestRows { get; init; }
        public required double[][] TestProbabilities { get; init; }
        public required int[] TestLabels { get; init; }
        public required int[] TrainLabels { get; init; }
        public required MulticlassMetrics? TrainMetrics { get; init; }
        public required MulticlassMetrics? TestMetrics { get; init; }
        public required ModelDescription Description { get; init; }
        public required List<EvaluationWarning> Warnings { get; init; }
    }
}
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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models
{
    public sealed class MulticlassMetricsResult
    {
        public string[] Classes { get; init; } = [];
        public int[][] ConfusionMatrix { get; init; } = [];
        public int Total { get; init; }
        public double Accuracy { get; init; }
        public double AccuracyLower { get; init; }
        public double AccuracyUpper { get; init; }
        public double BalancedAccuracy { get; init; }
        public double CohenKappa { get; init; }
        public double CohenKappaStandardError { get; init; }
        public double CohenKappaLower { get; init; }
        public double CohenKappaUpper { get; init; }
        public string KappaWeightingUsed { get; init; } = string.Empty;
        public double MatthewsCorrelation { get; init; }
        public double Beta { get; init; }
        public MulticlassClassRow[] PerClass { get; init; } = [];
        public MulticlassAverageRow Macro { get; init; } = new();
        public MulticlassAverageRow Micro { get; init; } = new();
        public MulticlassAverageRow Weighted { get; init; } = new();
        public ConfusedClassPair[] TopConfusions { get; init; } = [];
    }
}
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
    public sealed class RocCurveResult
    {
        public RocCurvePoint[] Points { get; init; } = [];
        public int TotalPoints { get; init; }
        public double Auc { get; init; }
        public double AucStandardError { get; init; }
        public double AucLower { get; init; }
        public double AucUpper { get; init; }
        public double Gini { get; init; }
        public double KolmogorovSmirnov { get; init; }
        public double KolmogorovSmirnovThreshold { get; init; }
        public double YoudenJ { get; init; }
        public double YoudenThreshold { get; init; }
        public double YoudenSensitivity { get; init; }
        public double YoudenSpecificity { get; init; }
        public int Positives { get; init; }
        public int Negatives { get; init; }
        public int SkippedRows { get; init; }
    }
}
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
    public sealed class ContingencyAssociationResult
    {
        public string[] RowLabels { get; init; } = [];
        public string[] ColumnLabels { get; init; } = [];
        public double Total { get; init; }
        public double[] RowTotals { get; init; } = [];
        public double[] ColumnTotals { get; init; } = [];
        public double[][] Expected { get; init; } = [];
        public double[][] PearsonResiduals { get; init; } = [];
        public double[][] AdjustedResiduals { get; init; } = [];
        public double[][] CellChiSquare { get; init; } = [];
        public double ChiSquare { get; init; }
        public int DegreesOfFreedom { get; init; }
        public double PValue { get; init; }
        public double LikelihoodRatioChiSquare { get; init; }
        public double LikelihoodRatioPValue { get; init; }
        public double YatesChiSquare { get; init; }
        public double YatesPValue { get; init; }
        public double CramersV { get; init; }
        public double CramersVBiasCorrected { get; init; }
        public double Phi { get; init; }
        public double SignedPhi { get; init; }
        public double ContingencyCoefficient { get; init; }
        public double TschuprowT { get; init; }
        public string EffectSizeLabel { get; init; } = string.Empty;
        public double SmallestExpectedCount { get; init; }
        public double FractionOfCellsExpectedBelowFive { get; init; }
        public string[] Warnings { get; init; } = [];
    }
}
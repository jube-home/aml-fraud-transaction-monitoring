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

using Jube.Charting.Core.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Charting.Diagrams.Models
{
    public enum CoefficientSort
    {
        None,
        Magnitude,
        Estimate,
        PValue
    }

    public sealed record CoefficientItem(string Name, double Estimate)
    {
        public double? Low { get; init; }
        public double? High { get; init; }
        public double? PValue { get; init; }
        public double? StandardError { get; init; }
    }

    public sealed record CoefficientDiagramSpec
    {
        public ChartOptions Options { get; init; } = new();
        public IReadOnlyList<CoefficientItem> Items { get; init; } = [];
        public CoefficientSort Sort { get; init; } = CoefficientSort.Magnitude;
        public bool LogScale { get; init; }
        public bool ShowOddsRatios { get; init; }
        public double ConfidenceLevel { get; init; } = 0.95;
        public double SignificanceLevel { get; init; } = 0.05;
        public string EstimateName { get; init; } = "Coefficient";
        public int MaxRows { get; init; } = 40;
    }

    public sealed record CoefficientRow(
        string Name,
        double Estimate,
        double? Low,
        double? High,
        double? PValue,
        bool Significant,
        bool Positive);
}
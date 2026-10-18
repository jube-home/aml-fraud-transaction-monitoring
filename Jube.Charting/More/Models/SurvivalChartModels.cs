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

namespace Jube.Charting.More.Models
{
    public sealed record SurvivalGroup(
        string Name,
        IReadOnlyList<double> Times,
        IReadOnlyList<bool> Events,
        string? Color = null,
        SurvivalCurve? Curve = null);

    public sealed record SurvivalChartSpec
    {
        public ChartOptions Options { get; init; } = new();
        public IReadOnlyList<SurvivalGroup> Groups { get; init; } = [];
        public double Confidence { get; init; } = 0.95;
        public ConfidenceMethod Method { get; init; } = ConfidenceMethod.LogLog;
        public bool Bands { get; init; } = true;
        public bool CensorMarks { get; init; } = true;
        public bool AtRisk { get; init; } = true;
        public bool MedianMarkers { get; init; } = true;
        public bool LogRankTest { get; init; } = true;
        public bool Cumulative { get; init; }
        public (double Min, double Max)? XDomain { get; init; }
        public Func<double, string>? XFormat { get; init; }
    }
}
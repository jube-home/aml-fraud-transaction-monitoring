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
    public sealed record ComponentItem(string Name, double ExplainedVariance, IReadOnlyList<double> Loadings);

    public sealed record ClusterItem(string Name, double Size, IReadOnlyList<double> Centroid);

    public sealed record ComponentsDiagramSpec
    {
        public ChartOptions Options { get; init; } = new();
        public IReadOnlyList<string> Features { get; init; } = [];
        public IReadOnlyList<ComponentItem> Components { get; init; } = [];
        public IReadOnlyList<ClusterItem> Clusters { get; init; } = [];
        public int MaxFeatureRows { get; init; } = 40;
        public int MaxComponentColumns { get; init; } = 12;
        public string ValueName { get; init; } = "Standardised value";
    }
}
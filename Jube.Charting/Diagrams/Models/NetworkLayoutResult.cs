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

namespace Jube.Charting.Diagrams.Models
{
    public sealed record LaidNode(
        int Index,
        int Layer,
        int Slot,
        string Label,
        double X,
        double Y,
        double HalfWidth,
        double HalfHeight,
        bool IsAggregate,
        int HiddenCount,
        NetworkRole Role,
        double? Bias,
        int SourceCount,
        double? Emphasis = null,
        string? EmphasisText = null);

    public sealed record LaidEdge(int From, int To, double Weight, bool IsAggregate, int Count);

    public sealed record NetworkLayoutResult(
        IReadOnlyList<LaidNode> Nodes,
        IReadOnlyList<LaidEdge> Edges,
        IReadOnlyList<double> ColumnX,
        double Width,
        double Height,
        double HeaderHeight,
        double LeftLabelWidth,
        double RightLabelWidth,
        int TotalNodes,
        long TotalEdges,
        int DrawnEdgesBeforeElision);
}
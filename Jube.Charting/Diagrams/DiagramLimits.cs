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

namespace Jube.Charting.Diagrams
{
    public static class DiagramLimits
    {
        public const int MaxCanvas = 4000;
        public const int MaxNetworkLayers = 16;
        public const int MaxNetworkInputNodes = 100_000;
        public const long MaxNetworkInputEdges = 50_000_000;
        public const int MaxVisibleNetworkNodes = 300;
        public const int DefaultMaxNodesPerLayer = 24;
        public const int DefaultMaxNetworkEdges = 450;
        public const int MaxTreeInputNodes = 200_000;
        public const int MaxTreeDrawnNodes = 400;
        public const int MaxTreeBranches = 12;
        public const int MaxGraphNodes = 150;
        public const int MaxGraphEdges = 1_000;
        public const int MaxCircularNodes = 40;
        public const int MaxCoefficientRows = 80;
        public const int MaxCoefficientInput = 2_000;
        public const int MaxForestTrees = 12;
        public const int MaxPanels = 24;
        public const int MaxPanelPoints = 400;
        public const int MaxFuzzyVariables = 12;
        public const int MaxFuzzySets = 8;
        public const int MaxFuzzyRules = 30;
        public const int MaxComponents = 40;
        public const int MaxFeatures = 200;
        public const int MaxClusters = 40;
    }
}
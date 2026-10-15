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

using System.Collections.Generic;
using Accord.Neuro;

namespace Jube.Engine.Exhaustive.Topology
{
    internal static class TopologyLayers
    {
        internal static int[] LayersParamsArray(List<int> layers)
        {
            var layersArray = new int[layers.Count + 1];

            for (var j = 0; j < layers.Count; j++)
            {
                layersArray[j] = layers[j];
            }

            layersArray[^1] = 1;

            return layersArray;
        }

        internal static bool TryGrowLayers(List<int> layers, int lastLayerWidth, int variableCount,
            int layerWidthLimitInputLayerFactor, int layerDepthLimit)
        {
            if (lastLayerWidth > variableCount * layerWidthLimitInputLayerFactor)
            {
                if (layers.Count >= layerDepthLimit)
                {
                    return false;
                }

                layers.Add(1);

                return true;
            }

            layers[^1] += 1;

            return true;
        }

        internal static ActivationNetwork TopologyRandomise(ActivationNetwork topologyNetwork)
        {
            var topologyRandomise = new NguyenWidrow(topologyNetwork);
            topologyRandomise.Randomize();

            return topologyNetwork;
        }
    }
}
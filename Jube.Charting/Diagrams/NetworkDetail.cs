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

using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    public static class NetworkDetails
    {
        public const int RaisedNodesPerLayer = 48;
        public const int RaisedEdges = 1200;
        public const int ElidedMaximumWidth = 120;
        public const long ElidedMaximumEdges = 20_000;

        public static string Describe(NetworkDetail detail)
        {
            return detail switch
            {
                NetworkDetail.Full => "full diagram with weight labels",
                NetworkDetail.NoWeightLabels => "full diagram, weight labels dropped",
                NetworkDetail.Elided => "large diagram, weakest connections and surplus nodes elided",
                NetworkDetail.Density => "density view",
                _ => "automatic"
            };
        }

        public static NetworkDetail Resolve(NetworkDiagramSpec spec)
        {
            return Plan(spec).Detail;
        }

        internal static NetworkPlan Plan(NetworkDiagramSpec spec)
        {
            var layers = spec.Layers;

            if (layers.Count < 2)
            {
                return new NetworkPlan(spec.Detail == NetworkDetail.Auto ? NetworkDetail.Full : spec.Detail, spec);
            }

            var weighted = layers.Skip(1).Any(l => l.Weights is not null);

            switch (spec.Detail)
            {
                case NetworkDetail.Full:
                    return new NetworkPlan(NetworkDetail.Full, spec);
                case NetworkDetail.NoWeightLabels:
                    return new NetworkPlan(NetworkDetail.NoWeightLabels, spec with { WeightLabelLimit = 0 });
                case NetworkDetail.Elided:
                    return new NetworkPlan(NetworkDetail.Elided, Raised(spec));
                case NetworkDetail.Density:
                    return new NetworkPlan(NetworkDetail.Density, spec);
            }

            var widest = layers.Max(l => l.Nodes.Count);
            long edges = 0;

            for (var l = 1; l < layers.Count; l++)
            {
                edges += (long)layers[l].Nodes.Count * layers[l - 1].Nodes.Count;
            }

            var standard = Math.Max(4,
                Math.Min(spec.MaxNodesPerLayer, DiagramLimits.MaxVisibleNetworkNodes / layers.Count));

            if (widest <= standard && edges <= Math.Max(10, spec.MaxEdges))
            {
                return edges <= spec.WeightLabelLimit
                    ? new NetworkPlan(NetworkDetail.Full, spec)
                    : new NetworkPlan(NetworkDetail.NoWeightLabels, spec with { WeightLabelLimit = 0 });
            }

            var raised = Math.Max(4,
                Math.Min(RaisedNodesPerLayer, DiagramLimits.MaxVisibleNetworkNodes / layers.Count));

            if (widest <= raised && edges <= RaisedEdges)
            {
                return new NetworkPlan(NetworkDetail.NoWeightLabels, Raised(spec));
            }

            if (!weighted || (widest <= ElidedMaximumWidth && edges <= ElidedMaximumEdges))
            {
                return new NetworkPlan(NetworkDetail.Elided, Raised(spec));
            }

            return new NetworkPlan(NetworkDetail.Density, spec);
        }

        private static NetworkDiagramSpec Raised(NetworkDiagramSpec spec)
        {
            return spec with
            {
                MaxNodesPerLayer = Math.Max(spec.MaxNodesPerLayer, RaisedNodesPerLayer),
                MaxEdges = Math.Max(spec.MaxEdges, RaisedEdges),
                WeightLabelLimit = 0
            };
        }
    }
}
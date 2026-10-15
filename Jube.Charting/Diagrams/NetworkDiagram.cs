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
using Jube.Charting.Interfaces;
using Jube.Charting.Exceptions;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Diagrams
{
    public sealed class NetworkDiagram(NetworkDiagramSpec spec) : IStyleModeChart
    {
        public NetworkDiagramSpec Spec { get; } = spec;

        public NetworkDetail Detail
        {
            get
            {
                Validate(Spec);

                return NetworkDetails.Plan(Spec).Detail;
            }
        }

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return new NetworkDiagram(Spec with { Options = Spec.Options with { StyleMode = mode } });
        }

        public string Render()
        {
            Validate(Spec);
            var plan = NetworkDetails.Plan(Spec);

            if (plan.Detail == NetworkDetail.Density)
            {
                return NetworkDensityRenderer.Render(plan.Spec);
            }

            return NetworkRenderer.Render(plan.Spec, NetworkLayout.Compute(plan.Spec));
        }

        public NetworkLayoutResult Layout()
        {
            Validate(Spec);

            return NetworkLayout.Compute(NetworkDetails.Plan(Spec).Spec);
        }

        private static void Validate(NetworkDiagramSpec spec)
        {
            for (var l = 0; l < spec.Layers.Count; l++)
            {
                var layer = spec.Layers[l];

                if (layer.Nodes.Count == 0)
                {
                    throw new ChartSpecificationException("Every network layer needs at least one node.");
                }

                foreach (var node in layer.Nodes)
                {
                    if (node.Bias is { } b && !double.IsFinite(b))
                    {
                        throw new ChartSpecificationException("Node biases must be finite.");
                    }

                    if (node.Emphasis is { } e && !(double.IsFinite(e) && e is >= 0 and <= 1))
                    {
                        throw new ChartSpecificationException("Node emphasis must be between 0 and 1.");
                    }
                }

                if (layer.Weights is null)
                {
                    continue;
                }

                if (l == 0)
                {
                    throw new ChartSpecificationException("The input layer cannot have incoming weights.");
                }

                if (layer.Weights.Count != layer.Nodes.Count)
                {
                    throw new ChartSpecificationException(
                        "Layer weights need one row per node in the layer: expected " + layer.Nodes.Count +
                        " but got " +
                        layer.Weights.Count + ".");
                }

                foreach (var row in layer.Weights)
                {
                    if (row.Count != spec.Layers[l - 1].Nodes.Count)
                    {
                        throw new ChartSpecificationException(
                            "Each weight row needs one weight per node in the previous layer: expected " +
                            spec.Layers[l - 1].Nodes.Count + " but got " + row.Count + ".");
                    }

                    if (row.Any(double.IsInfinity))
                    {
                        throw new ChartSpecificationException(
                            "Weights must be finite or NaN for an absent connection.");
                    }
                }
            }
        }
    }
}
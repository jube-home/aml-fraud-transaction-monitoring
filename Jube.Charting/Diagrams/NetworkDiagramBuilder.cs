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

using Jube.Charting.Core;
using Jube.Charting.Exceptions;
using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    public sealed class NetworkDiagramBuilder : ChartBuilder<NetworkDiagramBuilder>
    {
        private readonly List<NetworkLayer> layers = [];
        private NetworkDetail detail = NetworkDetail.Full;
        private string? emphasisTitle;
        private int maxEdges = DiagramLimits.DefaultMaxNetworkEdges;
        private int maxNodes = DiagramLimits.DefaultMaxNodesPerLayer;

        public NetworkDiagramBuilder Input(IEnumerable<string> labels, string? name = null)
        {
            layers.Add(new NetworkLayer
                { Name = name, Role = NetworkRole.Input, Nodes = [.. labels.Select(l => new NetworkNode(l))] });

            return this;
        }

        public NetworkDiagramBuilder Hidden(IEnumerable<string> labels, IReadOnlyList<IReadOnlyList<double>>? weights,
            string? activation = null, IReadOnlyList<double>? biases = null, string? name = null)
        {
            return Layer(NetworkRole.Hidden, labels, weights, activation, biases, name);
        }

        public NetworkDiagramBuilder Output(IEnumerable<string> labels, IReadOnlyList<IReadOnlyList<double>>? weights,
            string? activation = null, IReadOnlyList<double>? biases = null, string? name = null)
        {
            return Layer(NetworkRole.Output, labels, weights, activation, biases, name);
        }

        public NetworkDiagramBuilder Layer(NetworkRole role, IEnumerable<string> labels,
            IReadOnlyList<IReadOnlyList<double>>? weights, string? activation = null,
            IReadOnlyList<double>? biases = null, string? name = null)
        {
            var l = labels.ToArray();

            if (biases is not null && biases.Count != l.Length)
            {
                throw new ChartSpecificationException("Biases need one value per node.");
            }

            layers.Add(new NetworkLayer
            {
                Name = name,
                Activation = activation,
                Role = role,
                Weights = weights,
                Nodes = [.. l.Select((label, i) => new NetworkNode(label, biases?[i]))]
            });

            return this;
        }

        public NetworkDiagramBuilder InputEmphasis(string title, IEnumerable<double?> strengths,
            IEnumerable<string?>? texts = null)
        {
            return Emphasis(0, title, strengths, texts);
        }

        public NetworkDiagramBuilder Emphasis(int layerIndex, string title, IEnumerable<double?> strengths,
            IEnumerable<string?>? texts = null)
        {
            if (layerIndex < 0 || layerIndex >= layers.Count)
            {
                throw new ChartSpecificationException("Add the layer before its emphasis.");
            }

            var s = strengths.ToArray();
            var x = texts?.ToArray();
            var layer = layers[layerIndex];

            if (s.Length != layer.Nodes.Count || (x is not null && x.Length != s.Length))
            {
                throw new ChartSpecificationException("Emphasis needs one value per node in the layer.");
            }

            layers[layerIndex] = layer with
            {
                Nodes = [.. layer.Nodes.Select((n, i) => n with { Emphasis = s[i], EmphasisText = x?[i] })]
            };

            emphasisTitle = title;

            return this;
        }

        public NetworkDiagramBuilder Detail(NetworkDetail detailLevel)
        {
            detail = detailLevel;

            return this;
        }

        public NetworkDiagramBuilder MaxNodesPerLayer(int count)
        {
            maxNodes = count;

            return this;
        }

        public NetworkDiagramBuilder MaxEdges(int count)
        {
            maxEdges = count;

            return this;
        }

        public NetworkDiagram Build()
        {
            return new NetworkDiagram(new NetworkDiagramSpec
            {
                Options = Options, Layers = [.. layers], MaxNodesPerLayer = maxNodes, MaxEdges = maxEdges,
                EmphasisTitle = emphasisTitle, Detail = detail
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}
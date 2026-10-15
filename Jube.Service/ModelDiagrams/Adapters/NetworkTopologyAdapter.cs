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

using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class NetworkTopologyAdapter : IModelDiagramAdapter
    {
        public string Name => "NetworkTopology";

        public double? Detect(JToken model)
        {
            if (ModelJson.Get(model, "layers") is not JArray { Count: > 0 } layers)
            {
                return null;
            }

            foreach (var layer in layers)
            {
                if (ModelJson.Get(layer, "neurons") is not JArray { Count: > 0 } neurons)
                {
                    return null;
                }

                foreach (var neuron in neurons)
                {
                    if (ModelJson.Get(neuron, "weights") is not JArray || ModelJson.Number(neuron, "threshold") is null)
                    {
                        return null;
                    }
                }
            }

            var marker = ModelJson.String(layers[0], "$type");

            return SerialisationMarkers.Has(marker, SerialisationMarkers.NeuroNamespace) ? 0.99 : 0.9;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var layers = (JArray)ModelJson.Get(model, "layers")!;
            var weightLayers = new List<double[][]>();
            var biasLayers = new List<double[]>();
            var activations = new List<string?>();
            long elements = 0;

            foreach (var layer in layers)
            {
                var neurons = (JArray)ModelJson.Get(layer, "neurons")!;
                var weights = new double[neurons.Count][];
                var biases = new double[neurons.Count];
                var names = new HashSet<string>(StringComparer.Ordinal);

                for (var i = 0; i < neurons.Count; i++)
                {
                    weights[i] = ModelJson.Doubles(neurons[i], "weights")
                                 ?? throw ModelJson.Inconsistent("a neuron has weights that are not numbers");
                    biases[i] = ModelJson.Number(neurons[i], "threshold")
                                ?? throw ModelJson.Inconsistent("a neuron has a threshold that is not a number");

                    names.Add(ActivationName(ModelJson.Get(neurons[i], "activationFunction")));
                }

                weightLayers.Add(weights);
                biasLayers.Add(biases);
                activations.Add(names.Count == 1 ? names.First() : "Mixed");
                elements += neurons.Count;
            }

            var inputCount = weightLayers[0][0].Length;

            if (ModelJson.Integer(model, "inputsCount") is { } declared && declared != inputCount)
            {
                throw ModelJson.Inconsistent("the declared input count " + declared + " differs from the " +
                                             inputCount +
                                             " weights per first layer neuron");
            }

            for (var l = 0; l < weightLayers.Count; l++)
            {
                var expected = l == 0 ? inputCount : weightLayers[l - 1].Length;

                if (weightLayers[l].Any(w => w.Length != expected))
                {
                    throw ModelJson.Inconsistent("layer " + (l + 1) + " neurons need " + expected + " weights each");
                }
            }

            elements += inputCount;
            options.RequireNetworkWithin(elements);

            var stored = StoredNames(model, inputCount);
            var inputNames = options.Variables(stored, inputCount);

            var builder = Diagram.Network().Input(inputNames, "Inputs");

            for (var l = 0; l < weightLayers.Count; l++)
            {
                var last = l == weightLayers.Count - 1;
                var count = weightLayers[l].Length;

                var labels = last
                    ? count == 1 ? ["y"] : Enumerable.Range(1, count).Select(i => "y" + i).ToArray()
                    : Enumerable.Range(1, count).Select(i => "h" + (l + 1) + "." + i).ToArray();

                var rows = weightLayers[l].Select(w => (IReadOnlyList<double>)w).ToArray();

                builder.Layer(last ? NetworkRole.Output : NetworkRole.Hidden, labels, rows, activations[l],
                    biasLayers[l],
                    last ? "Output" : "Hidden " + (l + 1));
            }

            var subtitle = inputCount + " inputs, " +
                           string.Join(", ", weightLayers.Select((w, l) =>
                               w.Length + (l == weightLayers.Count - 1 ? " output" : " hidden") +
                               (w.Length == 1 ? "" : "s")));

            var built = builder.Build();

            return new NetworkDiagram(built.Spec with
            {
                Options = options.ToChartOptions("Feed-forward network", subtitle,
                    "Feed-forward network with " + weightLayers.Count + " weighted layers."),
                Detail = options.NetworkDetail
            });
        }

        private static string[]? StoredNames(JToken model, int inputCount)
        {
            if (ModelJson.Get(model, "annotations") is not JArray a || a.Count != inputCount)
            {
                return null;
            }

            var names = new string[a.Count];

            for (var i = 0; i < names.Length; i++)
            {
                var n = ModelJson.String(a[i], "name");

                if (string.IsNullOrWhiteSpace(n))
                {
                    return null;
                }

                names[i] = n;
            }

            return names;
        }

        internal static string ActivationName(JToken? function)
        {
            var type = ModelJson.String(function, "$type");

            if (type is null)
            {
                return "Unknown";
            }

            var comma = type.IndexOf(',', StringComparison.Ordinal);
            var full = comma >= 0 ? type[..comma] : type;
            var name = full[(full.LastIndexOf('.') + 1)..];

            if (name.EndsWith("Function", StringComparison.Ordinal) && name.Length > "Function".Length)
            {
                name = name[..^"Function".Length];
            }

            if (name.Length is 0 or > 32 || !name.All(char.IsAsciiLetterOrDigit))
            {
                return "Custom";
            }

            var alpha = ModelJson.Number(function, "alpha");

            return alpha is { } a && double.IsFinite(a) ? name + ", alpha " + ModelJson.Format(a) : name;
        }
    }
}
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
    public sealed class NeuralNetworkPipelineKindAdapter : ISnapshotKindAdapter
    {
        private readonly bool regressor;

        private NeuralNetworkPipelineKindAdapter(string kind, bool regressor)
        {
            Kind = kind;
            this.regressor = regressor;
        }

        public static IReadOnlyList<ISnapshotKindAdapter> All { get; } =
        [
            new NeuralNetworkPipelineKindAdapter("NeuralNetworkClassifierPipeline", false),
            new NeuralNetworkPipelineKindAdapter("NeuralNetworkRegressorPipeline", true)
        ];

        public string Kind { get; }

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var inputWeights = ModelJson.Matrix(payload, "inputWeights") ??
                               throw ModelJson.Inconsistent("the payload has no input weights");
            var hiddenBiases = ModelJson.Doubles(payload, "hiddenBiases") ??
                               throw ModelJson.Inconsistent("the payload has no hidden biases");
            var outputWeights = ModelJson.Doubles(payload, "outputWeights") ??
                                throw ModelJson.Inconsistent("the payload has no output weights");
            var outputBias = ModelJson.Number(payload, "outputBias") ??
                             throw ModelJson.Inconsistent("the payload has no output bias");

            var hidden = inputWeights.Length;

            if (hidden == 0 || hiddenBiases.Length != hidden || outputWeights.Length != hidden)
            {
                throw ModelJson.Inconsistent("the hidden layer needs one bias and one output weight per unit");
            }

            var inputCount = inputWeights[0].Length;

            if (inputCount == 0 || !ModelJson.IsRectangular(inputWeights, hidden, inputCount))
            {
                throw ModelJson.Inconsistent("every hidden unit needs one weight per input");
            }

            if (inputWeights.Any(r => r.Any(w => !double.IsFinite(w))) || hiddenBiases.Any(b => !double.IsFinite(b)) ||
                outputWeights.Any(w => !double.IsFinite(w)) || !double.IsFinite(outputBias))
            {
                throw ModelJson.Inconsistent("the network weights must be finite");
            }

            options.RequireNetworkWithin(inputCount + hidden + 1L);
            var names = options.Variables(ModelJson.Strings(envelope, "variablesInOrder"), inputCount);

            var standardise = ModelJson.Get(payload, "standardise") is { Type: JTokenType.Boolean } s &&
                              s.Value<bool>();

            var built = Diagram.Network()
                .Input(names, standardise ? "Standardised inputs" : "Inputs")
                .Layer(NetworkRole.Hidden, Enumerable.Range(1, hidden).Select(i => "h" + i),
                    inputWeights.Select(r => (IReadOnlyList<double>)r).ToArray(), "Sigmoid", hiddenBiases, "Hidden")
                .Layer(NetworkRole.Output, ["y"], [outputWeights], "Sigmoid", [outputBias], "Output")
                .Build();

            var output = regressor ? Rescale(payload) : "the probability of class 1";

            return new NetworkDiagram(built.Spec with
            {
                Options = options.ToChartOptions(
                    regressor ? "Neural network regressor" : "Neural network classifier",
                    inputCount + " inputs, " + hidden + " sigmoid hidden unit" + (hidden == 1 ? "" : "s") +
                    ", 1 output",
                    "A feed-forward network with one hidden layer of " + hidden +
                    " sigmoid units. The sigmoid output " +
                    "is " + output + "."),
                Detail = options.NetworkDetail
            });
        }

        private static string Rescale(JObject payload)
        {
            var low = ModelJson.Number(payload, "targetLow");
            var span = ModelJson.Number(payload, "targetSpan");

            return low is { } l && span is { } w && double.IsFinite(l) && double.IsFinite(w)
                ? "rescaled to the target range " + ModelJson.Format(l) + " to " + ModelJson.Format(l + w)
                : "rescaled to the target range";
        }
    }
}
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
    public sealed class HiddenMarkovAdapter : IModelDiagramAdapter
    {
        public string Name => "HiddenMarkov";

        public double? Detect(JToken model)
        {
            if (model is not JObject || Transitions(model) is not { Length: > 0 } transitions
                                     || !ModelJson.IsRectangular(transitions, transitions.Length, transitions.Length)
                                     || Initial(model) is null)
            {
                return null;
            }

            var emissions = ModelJson.Get(model, "emissions");

            return emissions is JArray { Count: > 0 } array &&
                   (array.All(e => e is JObject && ModelJson.Number(e, "mean") is not null)
                    || ModelJson.Matrix(emissions) is not null)
                ? 0.95
                : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var transitions = Probabilities(Transitions(model)!);
            var states = transitions.Length;
            var initial = Probabilities([Initial(model)!])[0];

            if (initial.Length != states)
            {
                throw ModelJson.Inconsistent("the initial probabilities must cover " + states + " states");
            }

            foreach (var row in transitions)
            {
                if (row.Any(p => !double.IsFinite(p) || p < 0 || p > 1.000001) || Math.Abs(row.Sum() - 1) > 0.01)
                {
                    throw ModelJson.Inconsistent("a transition row is not a probability distribution");
                }
            }

            var emissions = (JArray)ModelJson.Get(model, "emissions")!;

            if (emissions.Count != states)
            {
                throw ModelJson.Inconsistent("there must be one emission distribution per state");
            }

            var names = options.Variables(null, states, "State ");
            var builder = Diagram.Graph();
            var normal = emissions[0] is JObject;
            var symbols = normal ? null : Probabilities(ModelJson.Matrix(emissions)!);

            for (var s = 0; s < states; s++)
            {
                builder.Node("s" + s, names[s], GraphNodeRole.State,
                    Detail(s, initial, normal ? emissions[s] : null, symbols));
            }

            for (var from = 0; from < states; from++)
            {
                for (var to = 0; to < states; to++)
                {
                    if (transitions[from][to] > 0)
                    {
                        builder.Edge("s" + from, "s" + to, probability: Math.Min(1, transitions[from][to]));
                    }
                }
            }

            options.RequireWithin(states + (long)states * states, "states and transitions");
            var built = builder.Build();
            var emission = normal ? "normal emissions" : "discrete emissions";

            return new GraphDiagram(built.Spec with
            {
                Options = options.ToChartOptions("Hidden Markov model", states + " states with " + emission,
                    "Transition probabilities of a hidden Markov model with " + states + " states and " + emission +
                    ".")
            });
        }

        private static double[][]? Transitions(JToken model)
        {
            return ModelJson.Matrix(model, "logTransitions") ?? ModelJson.Matrix(model, "transitions");
        }

        private static double[]? Initial(JToken model)
        {
            return ModelJson.Doubles(model, "logInitial") ?? ModelJson.Doubles(model, "probabilities")
                ?? ModelJson.Doubles(model, "initial");
        }

        private static double[][] Probabilities(double[][] values)
        {
            var logarithm = values.Any(r => r.Any(v => v < 0));

            return logarithm ? values.Select(r => r.Select(Math.Exp).ToArray()).ToArray() : values;
        }

        private static string Detail(int state, double[] initial, JToken? normal, double[][]? symbols)
        {
            var text = "Initial probability " + ModelJson.Format(initial[state]);

            if (normal is not null)
            {
                var mean = ModelJson.Number(normal, "mean");

                var deviation = ModelJson.Number(normal, "standardDeviation") ??
                                (ModelJson.Number(normal, "variance") is { } v ? Math.Sqrt(v) : null);

                return text + ". Emits normal, mean " + ModelJson.Format(mean ?? double.NaN) + ", deviation " +
                       ModelJson.Format(deviation ?? double.NaN);
            }

            var top = symbols![state].Select((p, i) => (p, i)).OrderByDescending(x => x.p).ThenBy(x => x.i).Take(3)
                .Select(x => "symbol " + x.i + " " + ModelJson.Format(x.p));

            return text + ". Most likely emissions: " + string.Join(", ", top);
        }
    }
}
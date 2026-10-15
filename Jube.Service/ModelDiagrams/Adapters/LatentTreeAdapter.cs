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
    public sealed class LatentTreeAdapter : IModelDiagramAdapter
    {
        public string Name => "LatentTree";

        public double? Detect(JToken model)
        {
            return model is JObject
                   && ModelJson.Integers(model, "parents") is not null
                   && ModelJson.Integers(model, "states") is not null
                   && ModelJson.Integers(model, "featureNodes") is not null
                   && ModelJson.Matrix(model, "binEdges") is not null
                   && ModelJson.Get(model, "conditionals") is JArray
                ? 0.95
                : null;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var parents = ModelJson.Integers(model, "parents")!;
            var states = ModelJson.Integers(model, "states")!;
            var featureNodes = ModelJson.Integers(model, "featureNodes")!;
            var tables = (JArray)ModelJson.Get(model, "conditionals")!;
            var nodes = parents.Length;

            if (nodes < 2 || states.Length != nodes || tables.Count != nodes || parents[0] != -1)
            {
                throw ModelJson.Inconsistent("the tree needs a class node and a state count and table for every node");
            }

            for (var i = 1; i < nodes; i++)
            {
                if (parents[i] < 0 || parents[i] >= i)
                {
                    throw ModelJson.Inconsistent("node " + i + " must have a parent with a lower index");
                }
            }

            var nodeFeature = Enumerable.Repeat(-1, nodes).ToArray();

            for (var f = 0; f < featureNodes.Length; f++)
            {
                if (featureNodes[f] < 1 || featureNodes[f] >= nodes || nodeFeature[featureNodes[f]] != -1)
                {
                    throw ModelJson.Inconsistent("feature " + f + " refers to an invalid node");
                }

                nodeFeature[featureNodes[f]] = f;
            }

            var names = options.Variables(null, featureNodes.Length);
            var classPriors = tables[0] is JArray { Count: 1 } root ? ModelJson.Doubles(root[0]) : null;

            var builder = Diagram.Graph().Node("n0", "Class", GraphNodeRole.Class,
                classPriors is null
                    ? states[0] + " classes"
                    : "Class priors: " + string.Join(", ", classPriors.Select(ModelJson.Format)));

            var latent = 0;

            for (var i = 1; i < nodes; i++)
            {
                if (nodeFeature[i] >= 0)
                {
                    builder.Node("n" + i, names[nodeFeature[i]], GraphNodeRole.Feature, states[i] + " states");
                }
                else
                {
                    latent++;

                    builder.Node("n" + i, "Z" + latent, GraphNodeRole.Latent,
                        "Hidden variable with " + states[i] + " states");
                }
            }

            for (var i = 1; i < nodes; i++)
            {
                builder.Edge("n" + parents[i], "n" + i, Strength(ModelJson.Matrix(tables[i])));
            }

            return BayesStructure.Finish(builder, options, "Latent tree classifier",
                latent + " hidden variable" + (latent == 1 ? "" : "s") + " group the " + featureNodes.Length +
                " features",
                "Dependence strength", "Structure of a latent tree classifier with " + latent + " hidden variables.");
        }

        internal static double? Strength(double[][]? table)
        {
            if (table is null || table.Length < 2)
            {
                return null;
            }

            var best = 0.0;

            for (var a = 0; a < table.Length; a++)
            {
                for (var b = a + 1; b < table.Length; b++)
                {
                    if (table[a].Length != table[b].Length)
                    {
                        return null;
                    }

                    var distance = 0.0;

                    for (var s = 0; s < table[a].Length; s++)
                    {
                        distance += Math.Abs(table[a][s] - table[b][s]);
                    }

                    best = Math.Max(best, distance / 2);
                }
            }

            return double.IsFinite(best) ? best : null;
        }
    }
}
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
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal static class ClusterProfiles
    {
        public static ComponentsDiagram Build(JArray clusters, string vectorKey, string[]? storedNames,
            ModelDiagramOptions options, string title, string subtitle, string description, string valueName,
            Func<double[][], int, double[][]> transform)
        {
            if (clusters.Count == 0)
            {
                throw ModelJson.Inconsistent("the payload has no clusters");
            }

            var vectors = clusters.Select(c => ModelJson.Doubles(c, vectorKey) ??
                                               throw ModelJson.Inconsistent("a cluster has no " + vectorKey)).ToArray();

            var dimension = vectors[0].Length;

            if (dimension == 0 || vectors.Any(c => c.Length != dimension || c.Any(v => !double.IsFinite(v))))
            {
                throw ModelJson.Inconsistent(
                    "the cluster " + vectorKey + "s must be finite and have the same dimension");
            }

            options.RequireWithin((long)vectors.Length * dimension, "cluster values");
            var names = options.Variables(storedNames, dimension);
            var values = transform(vectors, dimension);
            var items = new List<ClusterItem>();

            for (var k = 0; k < vectors.Length; k++)
            {
                var size = ModelJson.Number(clusters[k], "proportion") ?? 1;
                var index = ModelJson.Integer(clusters[k], "index") ?? k;
                items.Add(new ClusterItem("Cluster " + (index + 1), size, values[k]));
            }

            return new ComponentsDiagram(new ComponentsDiagramSpec
            {
                Options = options.ToChartOptions(title, subtitle, description),
                Features = names,
                Clusters = items,
                ValueName = valueName
            });
        }

        public static double[][] Mismatch(double[][] modes, int dimension)
        {
            var result = modes.Select(_ => new double[dimension]).ToArray();

            if (modes.Length < 2)
            {
                return result;
            }

            for (var k = 0; k < modes.Length; k++)
            {
                for (var f = 0; f < dimension; f++)
                {
                    var differ = 0;

                    for (var other = 0; other < modes.Length; other++)
                    {
                        if (other != k && modes[other][f] != modes[k][f])
                        {
                            differ++;
                        }
                    }

                    result[k][f] = (double)differ / (modes.Length - 1);
                }
            }

            return result;
        }
    }
}
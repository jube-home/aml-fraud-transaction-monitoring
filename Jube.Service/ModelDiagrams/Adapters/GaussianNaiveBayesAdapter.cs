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
    public sealed class GaussianNaiveBayesAdapter : IModelDiagramAdapter
    {
        public string Name => "GaussianNaiveBayes";

        public double? Detect(JToken model)
        {
            if (model is not JObject || !BayesStructure.HasNumbers(model, "priors")
                                     || ModelJson.Get(model, "distributions") is not JArray { Count: > 0 } classes)
            {
                return null;
            }

            foreach (var row in classes)
            {
                if (row is not JArray { Count: > 0 } features)
                {
                    return null;
                }

                foreach (var feature in features)
                {
                    if (feature is not JObject || ModelJson.Number(feature, "mean") is null ||
                        (ModelJson.Number(feature, "standardDeviation") is null &&
                         ModelJson.Number(feature, "variance") is null))
                    {
                        return null;
                    }
                }
            }

            return 0.95;
        }

        public ISvgChart Build(JToken model, ModelDiagramOptions options)
        {
            var classes = (JArray)ModelJson.Get(model, "distributions")!;
            var priors = ModelJson.Doubles(model, "priors")!;
            var classCount = classes.Count;
            var featureCount = ((JArray)classes[0]).Count;

            if (priors.Length != classCount || classes.Any(c => ((JArray)c).Count != featureCount))
            {
                throw ModelJson.Inconsistent("the priors and distributions must cover the same classes and features");
            }

            options.RequireWithin((long)classCount * featureCount, "class and feature densities");
            var names = options.Variables(null, featureCount);
            var panels = new List<DistributionPanel>();

            for (var f = 0; f < featureCount; f++)
            {
                var series = new List<DistributionSeries>();

                for (var c = 0; c < classCount; c++)
                {
                    var d = ((JArray)classes[c])[f];
                    var mean = ModelJson.Number(d, "mean")!.Value;

                    var sd = ModelJson.Number(d, "standardDeviation") ??
                             Math.Sqrt(ModelJson.Number(d, "variance")!.Value);

                    series.Add(DistributionSeries.Normal("Class " + c + " (prior " + ModelJson.Format(priors[c]) + ")",
                        mean, sd));
                }

                panels.Add(new DistributionPanel(names[f], series));
            }

            return new DistributionPanels(new DistributionPanelsSpec
            {
                Options = options.ToChartOptions("Gaussian naive Bayes",
                    "Density of each feature within each class",
                    "Per class normal densities of " + featureCount + " features in a naive Bayes classifier."),
                Panels = panels
            });
        }
    }
}
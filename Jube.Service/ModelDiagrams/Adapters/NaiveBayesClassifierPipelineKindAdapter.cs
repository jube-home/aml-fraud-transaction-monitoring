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

using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class NaiveBayesClassifierPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "NaiveBayesClassifierPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var mean0 = ModelJson.Doubles(payload, "mean0") ?? throw ModelJson.Inconsistent("the payload has no mean0");
            var mean1 = ModelJson.Doubles(payload, "mean1") ?? throw ModelJson.Inconsistent("the payload has no mean1");
            var variance0 = ModelJson.Doubles(payload, "variance0") ??
                            throw ModelJson.Inconsistent("the payload has no variance0");
            var variance1 = ModelJson.Doubles(payload, "variance1") ??
                            throw ModelJson.Inconsistent("the payload has no variance1");
            var priorLogOdds = ModelJson.Number(payload, "priorLogOdds") ??
                               throw ModelJson.Inconsistent("the payload has no prior log-odds");

            var names = options.Variables(ModelJson.Strings(envelope, "variablesInOrder"), mean1.Length);
            var prior = 1 / (1 + Math.Exp(-priorLogOdds));

            return GaussianPanels.Build(names,
                [
                    new GaussianGroup("Class 0 (prior " + CoefficientMapping.Short(1 - prior) + ")", 1 - prior, mean0,
                        GaussianPanels.Deviations(variance0, "the class 0 variances")),
                    new GaussianGroup("Class 1 (prior " + CoefficientMapping.Short(prior) + ")", prior, mean1,
                        GaussianPanels.Deviations(variance1, "the class 1 variances"))
                ], false, options, "Gaussian naive Bayes",
                "Normal density of each variable within each class",
                "Class-conditional normal densities of " + names.Count + " variables in a Gaussian naive Bayes " +
                "classifier, which treats the variables as independent given the class. Where the two curves of a " +
                "variable part, that variable moves the score.");
        }
    }
}
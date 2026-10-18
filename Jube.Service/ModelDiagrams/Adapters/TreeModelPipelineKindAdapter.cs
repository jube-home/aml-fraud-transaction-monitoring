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
    public sealed class TreeModelPipelineKindAdapter : ISnapshotKindAdapter
    {
        private readonly Func<JObject, string> description;
        private readonly Func<JToken, NodeArrays> read;
        private readonly bool single;
        private readonly bool strictLess;
        private readonly TreeLeafStyle style;
        private readonly string title;
        private readonly string valueName;
        private readonly string? weightsKey;

        private TreeModelPipelineKindAdapter(string kind, bool single, TreeLeafStyle style, string title,
            string valueName, Func<JToken, NodeArrays> read, Func<JObject, string> description,
            string? weightsKey = null,
            bool strictLess = false)
        {
            Kind = kind;
            this.single = single;
            this.style = style;
            this.title = title;
            this.valueName = valueName;
            this.read = read;
            this.description = description;
            this.weightsKey = weightsKey;
            this.strictLess = strictLess;
        }

        public static IReadOnlyList<ISnapshotKindAdapter> All { get; } =
        [
            new TreeModelPipelineKindAdapter("DecisionTreeClassifierPipeline", true, TreeLeafStyle.Probability,
                "Decision tree classifier", "P(class 1)", t => SnapshotTrees.ByLeafFlag(t, "nodeProbability"),
                _ =>
                    "A binary decision tree. Each split sends rows at or below the threshold to the left (yes) branch; " +
                    "each node shows the share of class 1 among the training rows that reached it."),
            new TreeModelPipelineKindAdapter("DecisionTreeRegressorPipeline", true, TreeLeafStyle.Value,
                "Decision tree regressor", "prediction", t => SnapshotTrees.ByLeafFlag(t, "nodeValue"),
                _ => "A regression tree. Each split sends rows at or below the threshold to the left (yes) branch; " +
                     "each leaf predicts the mean target of the training rows that reached it."),
            new TreeModelPipelineKindAdapter("RandomForestClassifierPipeline", false, TreeLeafStyle.Probability,
                "Random forest classifier", "P(class 1)", Forest("threshold", "value"),
                p => Count(p) +
                     " decision trees grown on bootstrap samples; the score is the average of their class 1 " +
                     "probabilities. The importance strip is each variable's share of all splits in the forest."),
            new TreeModelPipelineKindAdapter("RandomForestRegressorPipeline", false, TreeLeafStyle.Value,
                "Random forest regressor", "prediction", Forest("threshold", "value"),
                p => Count(p) + " regression trees grown on bootstrap samples; the prediction is the average of " +
                     "their leaves. The importance strip is each variable's share of all splits in the forest."),
            new TreeModelPipelineKindAdapter("GradientBoostingClassifierPipeline", false, TreeLeafStyle.Value,
                "Gradient boosting classifier", "log-odds step", Forest("threshold", "value"),
                p => Count(p) + " boosted trees added in sequence: the log-odds start at " +
                     Number(p, "baseScore") + " and each tree's leaf adds its step times the learning rate " +
                     Number(p, "learningRate") + ". The importance strip is each variable's share of all splits."),
            new TreeModelPipelineKindAdapter("GradientBoostingRegressorPipeline", false, TreeLeafStyle.Value,
                "Gradient boosting regressor", "step", Forest("threshold", "value"),
                p => Count(p) + " boosted trees added in sequence: the prediction starts at " +
                     Number(p, "baseValue") + " and each tree's leaf adds its step times the learning rate " +
                     Number(p, "learningRate") + ". The importance strip is each variable's share of all splits."),
            new TreeModelPipelineKindAdapter("AdaBoostClassifierPipeline", false, TreeLeafStyle.Probability,
                "AdaBoost classifier", "weighted P(class 1)", t => SnapshotTrees.ByLeafFlag(t, "nodeVote"),
                p => Count(p) +
                     " boosted trees, each voting for the class holding the weighted majority of its leaf; " +
                     "the votes are combined with each tree's alpha. The importance strip is each variable's share " +
                     "of splits, weighted by the alpha of its tree.", "alphas"),
            new TreeModelPipelineKindAdapter("BaggingClassifierPipeline", false, TreeLeafStyle.Probability,
                "Bagging classifier", "P(class 1)", t => SnapshotTrees.ByLeafFlag(t, "nodeProbability"),
                p => Count(p) +
                     " decision trees grown on bootstrap samples; the score is the average of their class 1 " +
                     "probabilities. The importance strip is each variable's share of all splits."),
            new TreeModelPipelineKindAdapter("IsolationForestAnomalyPipeline", false, TreeLeafStyle.Size,
                "Isolation forest", "rows", Forest("splitValue", null, "size"),
                p => Count(p) + " isolation trees, each grown on " + Number(p, "subsampleSize") + " sampled rows " +
                     "with random splits; a row isolated after few splits scores as anomalous. Each node shows how " +
                     "many sampled rows reached it. The importance strip is each variable's share of all splits.",
                strictLess: true)
        ];

        public string Kind { get; }

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var stored = ModelJson.Strings(envelope, "variablesInOrder");
            var inputCount = stored?.Length ?? ModelJson.Integer(envelope, "inputCount") ?? 0;

            if (inputCount < 1)
            {
                throw ModelJson.Inconsistent("the snapshot lists no variables");
            }

            var reader = new SnapshotTrees(options.Variables(stored, inputCount), style, strictLess);

            if (single)
            {
                var tree = read(payload);

                return reader.Single(tree, options, title, tree.Count + " nodes over " + inputCount + " variables",
                    description(payload), valueName);
            }

            var trees = ModelJson.Get(payload, "trees") as JArray ??
                        throw ModelJson.Inconsistent("the payload has no trees");

            double[]? weights = null;

            if (weightsKey is not null)
            {
                weights = ModelJson.Doubles(payload, weightsKey) ??
                          throw ModelJson.Inconsistent("the payload has no " + weightsKey);
            }

            return reader.Forest(trees.Select(read).ToArray(), weights, options, title, description(payload),
                valueName, weightsKey is null ? "Share of splits" : "Alpha-weighted share of splits");
        }

        private static Func<JToken, NodeArrays> Forest(string thresholdKey, string? valueKey, string? sizeKey = null)
        {
            return t => SnapshotTrees.ByFeatureIndex(t, thresholdKey, valueKey, sizeKey);
        }

        private static string Count(JObject payload)
        {
            return ModelJson.Get(payload, "trees") is JArray trees ? trees.Count.ToString() : "The";
        }

        private static string Number(JObject payload, string name)
        {
            return ModelJson.Number(payload, name) is { } v && double.IsFinite(v) ? ModelJson.Format(v) : "?";
        }
    }
}
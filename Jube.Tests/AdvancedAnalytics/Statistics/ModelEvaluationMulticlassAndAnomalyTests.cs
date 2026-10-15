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

using System;
using System.Linq;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class ModelEvaluationMulticlassAndAnomalyTests
    {
        [Fact]
        [Trait("Covers", "EvaluateMulticlassClassifier")]
        public void MulticlassAlgorithmsRecoverBandedClasses()
        {
            var (x, labels) = ModelEvaluationData.Bands();
            var tools = new MulticlassEvaluationTools();

            foreach (var algorithm in new[]
                     {
                         MulticlassAlgorithm.DecisionTree, MulticlassAlgorithm.RandomForest,
                         MulticlassAlgorithm.NaiveBayes, MulticlassAlgorithm.KNearestNeighbours,
                         MulticlassAlgorithm.NeuralNetwork, MulticlassAlgorithm.MultinomialLogistic
                     })
            {
                var result =
                    tools.EvaluateMulticlassClassifier(x, labels, ModelEvaluationData.Names, algorithm, treeCount: 30);

                result.ClassLabels.Should().Equal(10, 20, 30);

                result.Test!.Accuracy.Should()
                    .BeGreaterThan(algorithm == MulticlassAlgorithm.MultinomialLogistic ? 0.6 : 0.85);

                result.Test.ConfusionMatrix.Select((row, i) => row.Sum() == result.Test.PerClass[i].Support).Should()
                    .OnlyContain(v => v);

                result.MajorityClassAccuracy.Should()
                    .BeApproximately(labels.GroupBy(l => l).Max(g => g.Count()) / (double)labels.Length, 1e-9);
            }

            ModelEvaluationData.Json(tools.EvaluateMulticlassClassifier(x, labels, ModelEvaluationData.Names,
                    MulticlassAlgorithm.RandomForest, treeCount: 30))
                .Should().Be(ModelEvaluationData.Json(tools.EvaluateMulticlassClassifier(x, labels,
                    ModelEvaluationData.Names, MulticlassAlgorithm.RandomForest, treeCount: 30)));

            tools.Invoking(t =>
                    t.EvaluateMulticlassClassifier(x, [.. labels.Select(l => l == 30 ? 20 : l)],
                        ModelEvaluationData.Names))
                .Should().Throw<ArgumentException>().Where(e => e.ParamName == "classLabels");
        }

        [Fact]
        [Trait("Covers", "EvaluateAnomalyDetector")]
        public void EveryDetectorScoresKnownOutliersHigher()
        {
            var (x, labels) = ModelEvaluationData.Anomalies();
            var tools = new AnomalyEvaluationTools();

            foreach (var algorithm in Enum.GetValues<AnomalyAlgorithm>())
            {
                var result =
                    tools.EvaluateAnomalyDetector(x, ["p", "q"], algorithm, labels, clusters: 1, seed: 2);

                result.RocAuc.Should().BeGreaterThan(algorithm == AnomalyAlgorithm.OneClassSupportVector ? 0.6 : 0.8,
                    algorithm.ToString());

                ModelEvaluationData
                    .Json(tools.EvaluateAnomalyDetector(x, ["p", "q"], algorithm, labels, clusters: 1, seed: 2))
                    .Should().Be(ModelEvaluationData.Json(result));
            }

            var supplied = tools.EvaluateAnomalyDetector(x, ["p", "q"], contamination: 0.1);
            supplied.HasLabels.Should().BeFalse();
            supplied.ContaminationUsed.Should().Be(0.1);
            supplied.ContaminationSource.Should().Be("Supplied");

            tools.Invoking(t => t.EvaluateAnomalyDetector(x, ["p", "q"], contamination: 0.9)).Should()
                .Throw<ArgumentException>().Where(e => e.ParamName == "contamination");
        }

        [Fact]
        [Trait("Covers", "EstimateAnomalyContamination")]
        public void ContaminationIsEstimatedFromASeparatedGroup()
        {
            var random = new Random(1);

            var scores = Enumerable.Range(0, 200).Select(i => i < 10 ? 10d + random.NextDouble() : random.NextDouble())
                .ToArray();

            var tools = new AnomalyEvaluationTools();
            var result = tools.EstimateAnomalyContamination(scores);
            result.RecommendedShare.Should().BeApproximately(0.05, 0.03);
            var flipped = tools.EstimateAnomalyContamination([.. scores.Select(s => -s)], false);
            flipped.RecommendedShare.Should().BeApproximately(0.05, 0.03);
            tools.EstimateAnomalyContamination([1, 2, 3]).RecommendedShare.Should().BeNull();

            tools.Invoking(t => t.EstimateAnomalyContamination([1d, double.PositiveInfinity])).Should()
                .Throw<ArgumentException>();
        }
    }
}
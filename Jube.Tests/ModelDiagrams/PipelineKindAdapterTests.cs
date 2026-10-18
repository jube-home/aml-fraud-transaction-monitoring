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
using Jube.Charting.Charts;
using Jube.Charting.Interfaces;
using Jube.Charting.Diagrams;
using Jube.Service.ModelDiagrams;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Test.ModelDiagrams
{
    public class PipelineKindAdapterTests
    {
        private static JObject Envelope(string kind, JObject payload, params string[] names)
        {
            return new JObject
            {
                ["kind"] = kind,
                ["schemaVersion"] = 1,
                ["inputCount"] = names.Length,
                ["variablesInOrder"] = new JArray([.. names.Cast<object>()]),
                ["payload"] = payload,
                ["calibrator"] = null
            };
        }

        private static T Build<T>(JObject envelope) where T : ISvgChart
        {
            var chart = ModelDiagramRenderer.Build(envelope.ToString());
            chart.Should().BeOfType<T>();
            chart.Render().Should().StartWith("<svg");

            return (T)chart;
        }

        private static JArray Rows(params double[][] rows)
        {
            return new JArray([.. rows.Select(r => new JArray([.. r.Cast<object>()])).Cast<object>()]);
        }

        private static JObject Mahalanobis(double[][] precision, int[] active, params string[] names)
        {
            return Envelope("MahalanobisAnomalyPipeline", new JObject
            {
                ["standardise"] = true,
                ["fillValues"] = new JArray([.. names.Select(_ => 0.0).Cast<object>()]),
                ["activeInputs"] = new JArray([.. active.Cast<object>()]),
                ["centre"] = new JArray([.. active.Select(_ => 0.0).Cast<object>()]),
                ["inverseCovariance"] = Rows(precision)
            }, names);
        }

        private static JObject Clusterer(string kind, string key, params double[][] vectors)
        {
            return Envelope(kind, new JObject
            {
                ["standardise"] = true,
                ["clusters"] = new JArray([
                    .. vectors.Select((v, i) => (object)new JObject
                    {
                        ["index"] = i, ["proportion"] = 1.0 / vectors.Length,
                        [key] = new JArray([.. v.Cast<object>()])
                    })
                ])
            }, "a", "b");
        }

        private static JObject DecisionTree(string kind, string valueKey)
        {
            return Envelope(kind, new JObject
            {
                ["fillValues"] = new JArray(0.0, 0.0),
                ["isLeaf"] = new JArray(false, true, false, true, true),
                ["attributeIndex"] = new JArray(1, 0, 0, 0, 0),
                ["threshold"] = new JArray(2.5, 0.0, 7.0, 0.0, 0.0),
                ["leftChild"] = new JArray(1, -1, 3, -1, -1),
                ["rightChild"] = new JArray(2, -1, 4, -1, -1),
                [valueKey] = new JArray(0.5, 0.2, 0.8, 0.6, 0.9)
            }, "amount", "velocity");
        }

        private static JObject Stump(int feature, double threshold, string thresholdKey = "threshold")
        {
            return new JObject
            {
                ["featureIndex"] = new JArray(feature, -1, -1),
                [thresholdKey] = new JArray(threshold, 0.0, 0.0),
                ["left"] = new JArray(1, -1, -1),
                ["right"] = new JArray(2, -1, -1),
                ["value"] = new JArray(0.5, 0.1, 0.9),
                ["size"] = new JArray(32, 20, 12)
            };
        }

        private static JObject Network(string kind)
        {
            return Envelope(kind, new JObject
            {
                ["standardise"] = true,
                ["hiddenUnits"] = 2,
                ["inputWeights"] = Rows([0.5, -1.0, 0.25], [2.0, 0.0, -0.75]),
                ["hiddenBiases"] = new JArray(0.1, -0.2),
                ["outputWeights"] = new JArray(1.5, -2.5),
                ["outputBias"] = 0.3,
                ["targetLow"] = 10.0,
                ["targetSpan"] = 90.0
            }, "amount", "velocity", "age");
        }

        [Fact]
        public void MahalanobisDrawsTheCorrelationOfTheInvertedPrecisionOverActiveVariables()
        {
            var inverse = new[] { new[] { 4.0 / 3, -2.0 / 3 }, new[] { -2.0 / 3, 4.0 / 3 } };

            var chart = Build<HeatmapChart>(Mahalanobis(inverse, [0, 2], "amount", "constant", "velocity"));

            chart.Spec.RowLabels.Should().Equal("amount", "velocity");
            chart.Spec.Values[0][1].Should().BeApproximately(0.5, 1e-9);
            chart.Spec.Values[1][1].Should().Be(1);
            chart.Spec.LegendTitle.Should().Be("Correlation");
            chart.Spec.Options.Description.Should().Contain("1 constant variable was left out");
        }

        [Fact]
        public void MahalanobisFallsBackToPartialCorrelationWhenThePrecisionIsSingular()
        {
            var chart = Build<HeatmapChart>(Mahalanobis([[1.0, 1.0], [1.0, 1.0]], [0, 1], "a", "b"));

            chart.Spec.LegendTitle.Should().Be("Partial correlation");
            chart.Spec.Values[0][1].Should().BeApproximately(-1, 1e-9);
        }

        [Fact]
        public void MahalanobisWithOneActiveVariableDrawsItsNormalProfile()
        {
            var chart = Build<DistributionPanels>(Mahalanobis([[4.0]], [1], "constant", "amount"));

            chart.Spec.Panels.Should().ContainSingle().Which.Title.Should().Be("amount");
            chart.Spec.Options.Description.Should().Contain("deviation 0.5");
        }

        [Fact]
        public void MahalanobisRejectsAPrecisionOfTheWrongSize()
        {
            var model = Mahalanobis([[1.0, 0.0], [0.0, 1.0]], [0], "a");

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*inverse covariance*");
        }

        [Fact]
        public void NaiveBayesDrawsBothClassDensitiesPerVariableWithPriorsFromTheLogOdds()
        {
            var chart = Build<DistributionPanels>(Envelope("NaiveBayesClassifierPipeline", new JObject
            {
                ["fillValues"] = new JArray(0.0, 0.0),
                ["mean1"] = new JArray(2.0, 0.0),
                ["variance1"] = new JArray(1.0, 4.0),
                ["mean0"] = new JArray(0.0, 0.0),
                ["variance0"] = new JArray(1.0, 1.0),
                ["priorLogOdds"] = 0.0
            }, "amount", "velocity"));

            chart.Spec.Panels.Select(p => p.Title).Should().Equal("amount", "velocity");

            chart.Spec.Panels[0].Series.Select(s => s.Name).Should()
                .Equal("Class 0 (prior 0.5)", "Class 1 (prior 0.5)");
        }

        [Fact]
        public void TreeAugmentedNaiveBayesLinksTheClassToEveryVariableAndEachVariableToItsParent()
        {
            var chart = Build<GraphDiagram>(Envelope("TreeAugmentedNaiveBayesClassifierPipeline", new JObject
            {
                ["fillValues"] = new JArray(0.0, 0.0, 0.0),
                ["parents"] = new JArray(-1, 0, 1),
                ["intercept1"] = new JArray(0.0, 0.0, 0.0),
                ["slope1"] = new JArray(0.0, 0.015, 2.0),
                ["variance1"] = new JArray(1.0, 1.0, 1.0),
                ["intercept0"] = new JArray(0.0, 0.0, 0.0),
                ["slope0"] = new JArray(0.0, -0.25, 1.0),
                ["variance0"] = new JArray(1.0, 1.0, 1.0),
                ["priorLogOdds"] = 0.0
            }, "a", "b", "c"));

            chart.Spec.Edges.Where(e => e.From == "class").Should().HaveCount(3);

            chart.Spec.Edges.Where(e => e.From != "class").Select(e => (e.From, e.To, e.Label)).Should()
                .Equal(("f0", "f1", "slope -0.25 / 0.015"), ("f1", "f2", "slope 1 / 2"));
        }

        [Fact]
        public void TreeAugmentedNaiveBayesRejectsACycle()
        {
            var model = Envelope("TreeAugmentedNaiveBayesClassifierPipeline", new JObject
            {
                ["parents"] = new JArray(1, 0),
                ["priorLogOdds"] = 0.0
            }, "a", "b");

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*cycle*");
        }

        [Fact]
        public void GaussianMixtureDensityRecoversMarginalDeviationsFromEachComponentPrecision()
        {
            var chart = Build<DistributionPanels>(Envelope("GaussianMixtureDensityPipeline", new JObject
            {
                ["standardise"] = false,
                ["activeInputs"] = new JArray(0),
                ["components"] = new JArray(
                    new JObject
                    {
                        ["weight"] = 0.25, ["mean"] = new JArray(0.0), ["inverseCovariance"] = Rows([0.25]),
                        ["logDeterminant"] = 0.0
                    },
                    new JObject
                    {
                        ["weight"] = 0.75, ["mean"] = new JArray(10.0), ["inverseCovariance"] = Rows([1.0]),
                        ["logDeterminant"] = 0.0
                    })
            }, "amount", "constant"));

            var panel = chart.Spec.Panels.Should().ContainSingle().Subject;
            panel.Series.Select(s => s.Name).Should().Equal("Mixture", "Component 1 (25%)", "Component 2 (75%)");
            panel.Series[0].X[0].Should().BeApproximately(-8, 1e-9);
            panel.Series[0].X[^1].Should().BeApproximately(14, 1e-9);
            chart.Spec.Options.Description.Should().Contain("1 constant variable is not part of the density");
        }

        [Fact]
        public void AMixtureWithMoreComponentsThanColoursDrawsTheHeaviestAndSaysSo()
        {
            var components = new JArray([
                .. Enumerable.Range(0, 10).Select(k => (object)new JObject
                {
                    ["index"] = k, ["mixtureWeight"] = (k + 1) / 55.0, ["mean"] = new JArray((double)k),
                    ["covariance"] = Rows([1.0])
                })
            ]);

            var chart = Build<DistributionPanels>(Envelope("GaussianMixtureClustererPipeline", new JObject
            {
                ["standardise"] = true, ["components"] = components
            }, "amount"));

            chart.Spec.Panels[0].Series.Should().HaveCount(8);

            chart.Spec.Panels[0].Series.Skip(1).Select(s => s.Name).Should().NotContain(n => n.StartsWith("Cluster 1 ")
                || n.StartsWith("Cluster 2 ") || n.StartsWith("Cluster 3 "));

            chart.Spec.Options.Subtitle.Should().Contain("the 7 heaviest of 10 components are drawn");
        }

        [Theory]
        [InlineData("BalancedKMeansClustererPipeline", "centroid", "Balanced k-means clusters")]
        [InlineData("BinarySplitClustererPipeline", "centroid", "Bisecting k-means clusters")]
        [InlineData("MiniBatchKMeansClustererPipeline", "centroid", "Mini-batch k-means clusters")]
        [InlineData("KMedoidsClustererPipeline", "medoid", "K-medoids clusters")]
        [InlineData("MeanShiftClustererPipeline", "centroid", "Mean-shift clusters")]
        public void CentroidClusterersStandardiseEachVariableAcrossClusters(string kind, string key, string title)
        {
            var chart = Build<ComponentsDiagram>(Clusterer(kind, key, [0, 10], [2, 10]));

            chart.Spec.Options.Title.Should().Be(title);

            chart.Spec.Clusters.Select(c => c.Centroid.ToArray()).Should()
                .BeEquivalentTo([[-1.0, 0.0], new[] { 1.0, 0.0 }], o => o.WithStrictOrdering());
        }

        [Fact]
        public void ASingleClusterShowsItsOwnValuesInsteadOfADegenerateZScore()
        {
            var chart = Build<ComponentsDiagram>(Clusterer("MeanShiftClustererPipeline", "centroid", [0.5, -2]));

            chart.Spec.Clusters.Single().Centroid.Should().Equal(0.5, -2);
            chart.Spec.ValueName.Should().Be("Mode value");
            chart.Spec.Options.Subtitle.Should().StartWith("1 cluster;");
        }

        [Fact]
        public void KModesShowsTheShareOfOtherClustersWhoseModeDiffers()
        {
            var chart = Build<ComponentsDiagram>(Clusterer("KModesClustererPipeline", "mode", [1, 4], [1, 5], [2, 4]));

            chart.Spec.Clusters.Select(c => c.Centroid.ToArray()).Should().BeEquivalentTo(
                [[0.5, 0.5], [0.5, 1.0], new[] { 1.0, 0.5 }], o => o.WithStrictOrdering());
        }

        [Fact]
        public void ADecisionTreeClassifierSplitsLeftAtOrBelowTheThresholdAndCarriesClassMixes()
        {
            var root = Build<TreeDiagram>(DecisionTree("DecisionTreeClassifierPipeline", "nodeProbability")).Spec
                .Root!;

            root.Feature.Should().Be("velocity");
            root.Threshold.Should().Be(2.5);
            root.Branches[0].Node.ClassCounts![0].Should().BeApproximately(0.8, 1e-12);
            root.Branches[0].Node.ClassCounts![1].Should().BeApproximately(0.2, 1e-12);
            root.Branches[1].Node.Feature.Should().Be("amount");
            root.Branches[1].Node.Branches[1].Node.ClassCounts![1].Should().BeApproximately(0.9, 1e-12);
        }

        [Fact]
        public void ADecisionTreeRegressorCarriesLeafPredictions()
        {
            var chart = Build<TreeDiagram>(DecisionTree("DecisionTreeRegressorPipeline", "nodeValue"));

            chart.Spec.ValueName.Should().Be("prediction");
            chart.Spec.Root!.Branches[0].Node.Value.Should().Be(0.2);
            chart.Spec.ClassNames.Should().BeEmpty();
        }

        [Fact]
        public void ATreeWhoseChildrenLoopBackIsRejected()
        {
            var model = DecisionTree("DecisionTreeRegressorPipeline", "nodeValue");
            model["payload"]!["rightChild"] = new JArray(2, -1, 0, -1, -1);

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*shared*");
        }

        [Fact]
        public void ARandomForestDrawsItsFirstTreesAndTheShareOfSplitsOverAllOfThem()
        {
            var trees = new JArray([.. Enumerable.Range(0, 9).Select(t => (object)Stump(t < 6 ? 0 : 1, t))]);

            var chart = Build<ForestOfTrees>(Envelope("RandomForestClassifierPipeline", new JObject
            {
                ["fillValues"] = new JArray(0.0, 0.0), ["trees"] = trees
            }, "amount", "velocity"));

            chart.Spec.Trees.Should().HaveCount(6);
            chart.Spec.TotalTrees.Should().Be(9);

            chart.Spec.Importances.Select(i => (i.Name, Math.Round(i.Value, 4))).Should()
                .Equal(("amount", 0.6667), ("velocity", 0.3333));
        }

        [Fact]
        public void AdaBoostWeightsTheShareOfSplitsByEachTreesAlpha()
        {
            JObject Tree(int feature)
            {
                return new JObject
                {
                    ["isLeaf"] = new JArray(false, true, true), ["attributeIndex"] = new JArray(feature, 0, 0),
                    ["threshold"] = new JArray(1.0, 0.0, 0.0), ["leftChild"] = new JArray(1, -1, -1),
                    ["rightChild"] = new JArray(2, -1, -1), ["nodeVote"] = new JArray(0.5, 0.2, 0.7)
                };
            }

            var chart = Build<ForestOfTrees>(Envelope("AdaBoostClassifierPipeline", new JObject
            {
                ["trees"] = new JArray(Tree(0), Tree(1)), ["alphas"] = new JArray(3.0, 1.0)
            }, "amount", "velocity"));

            chart.Spec.Importances.Select(i => (i.Name, i.Value)).Should().Equal(("amount", 0.75), ("velocity", 0.25));
            chart.Spec.ImportanceName.Should().Be("Alpha-weighted share of splits");
        }

        [Fact]
        public void AnIsolationForestSplitsStrictlyBelowTheValueAndCountsRows()
        {
            var chart = Build<ForestOfTrees>(Envelope("IsolationForestAnomalyPipeline", new JObject
            {
                ["subsampleSize"] = 32, ["trees"] = new JArray(Stump(1, 4.5, "splitValue"))
            }, "amount", "velocity"));

            var root = chart.Spec.Trees.Single().Root!;
            root.Condition.Should().Be("velocity < 4.5");
            root.Samples.Should().Be(32);
            root.Branches[1].Node.Samples.Should().Be(12);
            chart.Spec.Options.Description.Should().Contain("32 sampled rows");
        }

        [Fact]
        public void GradientBoostingStatesItsBaseScoreAndLearningRate()
        {
            var chart = Build<ForestOfTrees>(Envelope("GradientBoostingClassifierPipeline", new JObject
            {
                ["baseScore"] = -1.25, ["learningRate"] = 0.1, ["trees"] = new JArray(Stump(0, 1))
            }, "amount", "velocity"));

            chart.Spec.Options.Description.Should().Contain("start at -1.25").And.Contain("learning rate 0.1");
            chart.Spec.Trees[0].Root!.Branches[0].Node.Value.Should().Be(0.1);
        }

        [Theory]
        [InlineData("NeuralNetworkClassifierPipeline", "the probability of class 1")]
        [InlineData("NeuralNetworkRegressorPipeline", "rescaled to the target range 10 to 100")]
        public void NeuralNetworksDrawInputsHiddenUnitsAndOneOutputWithTheirWeights(string kind, string output)
        {
            var chart = Build<NetworkDiagram>(Network(kind));

            chart.Spec.Layers.Select(l => l.Nodes.Count).Should().Equal(3, 2, 1);
            chart.Spec.Layers[0].Nodes.Select(n => n.Label).Should().Equal("amount", "velocity", "age");
            chart.Spec.Layers[1].Weights![1].Should().Equal(2.0, 0.0, -0.75);
            chart.Spec.Layers[2].Weights![0].Should().Equal(1.5, -2.5);
            chart.Spec.Layers[2].Nodes[0].Bias.Should().Be(0.3);
            chart.Spec.Options.Description.Should().Contain(output);
        }

        [Fact]
        public void ANetworkWithMismatchedOutputWeightsIsRejected()
        {
            var model = Network("NeuralNetworkClassifierPipeline");
            model["payload"]!["outputWeights"] = new JArray(1.0);

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*output weight per unit*");
        }

        [Fact]
        public void MultipleLinearRegressionOmitsConstantsAndAddsMissingIndicators()
        {
            var chart = Build<CoefficientDiagram>(Envelope("MultipleLinearRegressionPipeline", new JObject
            {
                ["standardise"] = true,
                ["standardDeviations"] = new JArray(1.0, 0.0, 2.0),
                ["indicatorInputs"] = new JArray(2),
                ["weights"] = new JArray(1.5, 0.0, -0.5, 0.25),
                ["intercept"] = 7.0
            }, "amount", "constant", "velocity"));

            chart.Spec.Items.Select(i => (i.Name, i.Estimate)).Should()
                .Equal(("amount", 1.5), ("velocity", -0.5), ("velocity is missing", 0.25));

            chart.Spec.EstimateName.Should().Be("Standardised coefficient");
            chart.Spec.Options.Subtitle.Should().Be("Intercept 7.");
            chart.Spec.Options.Description.Should().Contain("1 constant variable omitted");
        }

        [Fact]
        public void MultipleLinearRegressionPrefersEvidenceWithStandardErrors()
        {
            var model = Envelope("MultipleLinearRegressionPipeline", new JObject
            {
                ["standardise"] = false, ["weights"] = new JArray(9.0), ["intercept"] = 1.0
            }, "amount");

            model["evidence"] = new JObject
            {
                ["coefficients"] = new JArray(
                    new JObject { ["name"] = "(Intercept)", ["kind"] = "Intercept", ["estimate"] = 1.0 },
                    new JObject
                    {
                        ["name"] = "amount", ["kind"] = "Predictor", ["estimate"] = 2.0, ["standardError"] = 0.5,
                        ["pValue"] = 0.01
                    },
                    new JObject { ["name"] = "constant", ["kind"] = "Predictor", ["estimate"] = 0.0 },
                    new JObject
                    {
                        ["name"] = "amount__missing", ["kind"] = "MissingIndicator", ["estimate"] = -0.5,
                        ["standardError"] = 0.25
                    })
            };

            var chart = Build<CoefficientDiagram>(model);

            chart.Spec.Items.Select(i => (i.Name, i.Estimate, i.StandardError, i.PValue)).Should()
                .Equal(("amount", 2.0, 0.5, 0.01), ("amount is missing", -0.5, 0.25, null));

            chart.Spec.Options.Description.Should().Contain("1 constant variable omitted");
        }

        [Fact]
        public void ALinearSupportVectorClassifierDrawsItsHyperplaneWeights()
        {
            var chart = Build<CoefficientDiagram>(Envelope("LinearSupportVectorClassifierPipeline", new JObject
            {
                ["standardise"] = false, ["weights"] = new JArray(0.75, -1.25), ["bias"] = -0.5
            }, "amount", "velocity"));

            chart.Spec.Items.Select(i => i.Estimate).Should().Equal(0.75, -1.25);
            chart.Spec.EstimateName.Should().Be("Weight per unit");
            chart.Spec.Options.Subtitle.Should().Be("Bias -0.5.");
        }

        [Fact]
        public void KNearestNeighboursClassifierProjectsReferenceRowsByClass()
        {
            var chart = Build<ScatterChart>(Envelope("KNearestNeighboursClassifierPipeline", new JObject
            {
                ["standardise"] = true,
                ["rows"] = Rows([0, 0], [1, 1], [2, 2.1], [3, 2.9]),
                ["labels"] = new JArray(0, 0, 1, 1),
                ["neighbourCount"] = 3
            }, "amount", "velocity"));

            chart.Spec.Series.Select(s => (s.Name, s.Points.Count)).Should().Equal(("Class 0", 2), ("Class 1", 2));
            chart.Spec.Options.XAxisTitle.Should().StartWith("Principal component 1 (");
            chart.Spec.Options.Subtitle.Should().Contain("k = 3");
            chart.Spec.Series[0].Points[0].X.Should().BeLessThan(chart.Spec.Series[1].Points[1].X);
        }

        [Fact]
        public void KNearestNeighboursRegressorGroupsRowsIntoTargetQuartersAndThinsLargeSets()
        {
            var rows = Enumerable.Range(0, 1000).Select(i => new double[] { i, i % 7 }).ToArray();

            var chart = Build<ScatterChart>(Envelope("KNearestNeighboursRegressorPipeline", new JObject
            {
                ["standardise"] = false,
                ["rows"] = Rows(rows),
                ["targets"] = new JArray([.. Enumerable.Range(0, 1000).Select(i => (object)(double)i)]),
                ["neighbourCount"] = 5
            }, "amount", "velocity"));

            chart.Spec.Series.Should().HaveCount(4);
            chart.Spec.Series.Sum(s => s.Points.Count).Should().BeLessThanOrEqualTo(400);
            chart.Spec.Series[0].Name.Should().Be("Target 0 to 249");
            chart.Spec.Options.Subtitle.Should().Contain("every 3rd row drawn");
        }

        [Fact]
        public void AKernelSupportVectorClassifierSplitsSupportVectorsByTheClassTheyPullTowards()
        {
            var chart = Build<ScatterChart>(Envelope("KernelSupportVectorClassifierPipeline", new JObject
            {
                ["supportVectors"] = Rows([0, 1], [1, 0], [2, 2]),
                ["coefficients"] = new JArray(-0.5, 0.25, 1.0),
                ["bias"] = 0.1, ["kernel"] = "Gaussian", ["gamma"] = 0.5, ["degree"] = 3
            }, "amount", "velocity"));

            chart.Spec.Series.Select(s => (s.Name, s.Points.Count)).Should()
                .Equal(("Class 0 support vectors", 1), ("Class 1 support vectors", 2));

            chart.Spec.Options.Subtitle.Should().Be("3 support vectors, Gaussian kernel, gamma 0.5");
            chart.Spec.Series[0].Points[0].Label.Should().Be("Support vector 1, coefficient -0.5");
        }

        [Fact]
        public void AHostileKernelNameIsNotEchoed()
        {
            var chart = Build<ScatterChart>(Envelope("KernelRidgeRegressorPipeline", new JObject
            {
                ["supportRows"] = Rows([0, 1], [1, 0]), ["alpha"] = new JArray(0.5, -0.5), ["mean"] = 3.0,
                ["kernel"] = "<script>alert(1)</script>", ["gamma"] = 1.0, ["degree"] = 2
            }, "amount", "velocity"));

            chart.Spec.Options.Subtitle.Should().Be("2 support rows, custom kernel, gamma 1");
            chart.Render().Should().NotContain("<script>");
        }

        [Fact]
        public void AOneClassSupportVectorModelDrawsItsSupportVectors()
        {
            var chart = Build<ScatterChart>(Envelope("OneClassSupportVectorAnomalyPipeline", new JObject
            {
                ["supportVectors"] = Rows([0, 1, 2], [1, 0, 2], [2, 2, 0]),
                ["alphas"] = new JArray(0.2, 0.3, 0.5), ["bias"] = 0.4, ["gamma"] = 0.25
            }, "amount", "velocity", "age"));

            chart.Spec.Series.Should().ContainSingle().Which.Points.Should().HaveCount(3);
            chart.Spec.Options.Subtitle.Should().Be("3 support vectors, Gaussian kernel, gamma 0.25");
        }

        [Fact]
        public void SupportSetsWithRowsOfTheWrongWidthAreRejected()
        {
            var model = Envelope("OneClassSupportVectorAnomalyPipeline", new JObject
            {
                ["supportVectors"] = Rows([0, 1], [1]), ["alphas"] = new JArray(0.5, 0.5), ["gamma"] = 1.0
            }, "amount", "velocity");

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*2 finite values*");
        }
    }
}
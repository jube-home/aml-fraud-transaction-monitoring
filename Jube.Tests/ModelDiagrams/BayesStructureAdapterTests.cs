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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Service.ModelDiagrams;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Test.ModelDiagrams
{
    public class BayesStructureAdapterTests
    {
        private static readonly string[] names = ["amount", "velocity", "merchant", "geo"];

        private static GraphDiagramSpec Spec(string json, int count = 4)
        {
            var options = new ModelDiagramOptions { VariableNames = [.. names.Take(count)] };

            return ((GraphDiagram)ModelDiagramRenderer.Build(json, options)).Spec;
        }

        private static IEnumerable<(string From, string To, double? Weight)> Edges(GraphDiagramSpec spec)
        {
            var labels = spec.Nodes.ToDictionary(n => n.Id, n => n.Label);

            return [.. spec.Edges.Select(e => (labels[e.From], labels[e.To], e.Weight))];
        }

        private static string Tan(int[] parents, double[][] information)
        {
            var n = parents.Length;

            double[] Row()
            {
                return new double[n];
            }

            return new JObject
            {
                ["priors"] = new JArray(0.4, 0.6),
                ["parents"] = new JArray(parents),
                ["intercepts"] = new JArray(new JArray(Row()), new JArray(Row())),
                ["slopes"] = new JArray(new JArray(Row()), new JArray(Row())),
                ["variances"] = new JArray(new JArray(Row()), new JArray(Row())),
                ["mutualInformation"] = new JArray(information.Select(r => new JArray(r)))
            }.ToString();
        }

        private static string KDependence(int[] order, int[][] parents)
        {
            var n = order.Length;

            return new JObject
            {
                ["k"] = parents.Max(p => p.Length),
                ["order"] = new JArray(order),
                ["parents"] = new JArray(parents.Select(p => new JArray(p))),
                ["numberOfStates"] = new JArray(Enumerable.Repeat(2, n)),
                ["binEdges"] = new JArray(Enumerable.Range(0, n).Select(_ => new JArray(0.0))),
                ["priors"] = new JArray(0.5, 0.5)
            }.ToString();
        }

        private static string Aode(double[] weights)
        {
            var n = weights.Length;

            return new JObject
            {
                ["minimumSupport"] = 1.0,
                ["weighted"] = true,
                ["estimatorWeights"] = new JArray(weights),
                ["superParentSupport"] = new JArray(Enumerable.Range(0, n).Select(_ => new JArray(new double[n]))),
                ["numberOfStates"] = new JArray(Enumerable.Repeat(2, n)),
                ["pairConditionals"] = new JArray(),
                ["priors"] = new JArray(0.5, 0.5)
            }.ToString();
        }

        private static string Hidden(double[][] weights)
        {
            return new JObject
            {
                ["numberOfStates"] = new JArray(Enumerable.Repeat(2, weights.Length)),
                ["pairConditionals"] = new JArray(),
                ["parentWeights"] = new JArray(weights.Select(r => new JArray(r))),
                ["priors"] = new JArray(0.5, 0.5)
            }.ToString();
        }

        private static string Latent(int[] parents, int[] states, int[] featureNodes, double[][]?[] tables)
        {
            var conditionals = new JArray();

            for (var i = 0; i < parents.Length; i++)
            {
                conditionals.Add(i == 0
                    ? new JArray(new JArray(Enumerable.Repeat(1.0 / states[0], states[0])))
                    : new JArray(tables[i]!.Select(r => new JArray(r))));
            }

            return new JObject
            {
                ["parents"] = new JArray(parents),
                ["states"] = new JArray(states),
                ["conditionals"] = conditionals,
                ["featureNodes"] = new JArray(featureNodes),
                ["nodeFeatures"] = new JArray(),
                ["binEdges"] = new JArray(featureNodes.Select(f =>
                    new JArray(Enumerable.Range(0, states[f] - 1).Select(x => (double)x)))),
                ["missingStates"] = new JArray(featureNodes.Select(_ => false))
            }.ToString();
        }

        [Fact]
        public void ATanParentArrayProducesExactlyTheClassEdgesAndTheTreeEdges()
        {
            var json = Tan([-1, 0, 0, 2],
                [[0, 0.1, 0.2, 0], [0.1, 0, 0, 0], [0.2, 0, 0, 0.3], [0, 0, 0.3, 0]]);

            var edges = Edges(Spec(json));

            edges.Should().BeEquivalentTo(new (string, string, double?)[]
            {
                ("Class", "amount", null), ("Class", "velocity", null), ("Class", "merchant", null),
                ("Class", "geo", null),
                ("amount", "velocity", 0.1), ("amount", "merchant", 0.2), ("merchant", "geo", 0.3)
            });
        }

        [Fact]
        public void ATanEdgeWeightIsTheChildsRowOfTheInformationMatrix()
        {
            var json = Tan([-1, 0, 1, 2], [[0, 0.5, 0, 0], [0.9, 0, 0.6, 0], [0, 0.7, 0, 0.8], [0, 0, 0.11, 0]]);

            var weights = Edges(Spec(json)).Where(e => e.From != "Class")
                .ToDictionary(e => e.From + ">" + e.To, e => e.Weight);

            weights.Should().Equal(new Dictionary<string, double?>
            {
                ["amount>velocity"] = 0.9, ["velocity>merchant"] = 0.7, ["merchant>geo"] = 0.11
            });
        }

        [Fact]
        public void ATanGraphNamesTheClassAndItsPriors()
        {
            var spec = Spec(Tan([-1, 0], [[0, 1], [1, 0]]), 2);

            spec.Nodes.Single(n => n.Role == GraphNodeRole.Class).Detail.Should().Be("Class priors: 0.4, 0.6");

            spec.Nodes.Where(n => n.Role == GraphNodeRole.Feature).Select(n => n.Label).Should()
                .Equal("amount", "velocity");

            spec.WeightName.Should().Be("Conditional mutual information");
            spec.Directed.Should().BeTrue();
        }

        [Fact]
        public void ATanCycleIsMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(Tan([1, 0], [[0, 1], [1, 0]]));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*cycle*");
        }

        [Fact]
        public void ATanSelfParentIsMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(Tan([-1, 1], [[0, 1], [1, 0]]));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*invalid parent*");
        }

        [Fact]
        public void ATanInformationMatrixOfTheWrongSizeIsMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(Tan([-1, 0, 0], [[0, 1], [1, 0]]));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*3 by 3*");
        }

        [Fact]
        public void TheRealTanFixtureHasOneClassEdgePerFeatureAndOneTreeEdgePerParent()
        {
            var model = ModelDiagramFixtures.Parsed("bayes-tan");
            var parents = model["parents"]!.Select(p => (int)p).ToArray();

            var spec = Spec(model.ToString());

            spec.Edges.Count(e => e.From == "class").Should().Be(4);
            spec.Edges.Count(e => e.From != "class").Should().Be(parents.Count(p => p >= 0));

            foreach (var edge in spec.Edges.Where(e => e.From != "class"))
            {
                var child = int.Parse(edge.To[1..]);
                edge.From.Should().Be("f" + parents[child]);
                edge.Weight.Should().Be((double)model["mutualInformation"]![child]![parents[child]]!);
            }
        }

        [Fact]
        public void AkDependenceParentListsProduceEveryFeatureToFeatureEdge()
        {
            var edges = Edges(Spec(KDependence([2, 0, 1], [[2], [2, 0], []]), 3));

            edges.Should().BeEquivalentTo(new (string, string, double?)[]
            {
                ("Class", "amount", null), ("Class", "velocity", null), ("Class", "merchant", null),
                ("merchant", "amount", null), ("merchant", "velocity", null), ("amount", "velocity", null)
            });
        }

        [Fact]
        public void AkDependenceOrderThatIsNotAPermutationIsMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(KDependence([0, 0, 1], [[], [], []]));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*permutation*");
        }

        [Fact]
        public void AkDependenceParentOutOfRangeIsMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(KDependence([0, 1], [[], [5]]));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*invalid parent 5*");
        }

        [Fact]
        public void TheRealKDependenceFixtureKeepsEveryStoredParent()
        {
            var model = ModelDiagramFixtures.Parsed("bayes-kdb");
            var parents = model["parents"]!.Select(r => r.Select(p => (int)p).ToArray()).ToArray();

            var spec = Spec(model.ToString());

            var stored = parents.SelectMany((ps, j) => ps.Select(p => "f" + p + ">f" + j)).OrderBy(x => x).ToArray();

            spec.Edges.Where(e => e.From != "class").Select(e => e.From + ">" + e.To).OrderBy(x => x).Should()
                .Equal(stored);

            spec.Options.Subtitle.Should().Contain("up to " + (int)model["k"]!);
        }

        [Fact]
        public void AveragedOneDependenceDrawsEachSuperParentToEveryOtherFeatureWithItsEstimatorWeight()
        {
            var edges = Edges(Spec(Aode([1, 0, 0.5]), 3)).Where(e => e.From != "Class").ToArray();

            edges.Should().BeEquivalentTo(new (string, string, double?)[]
            {
                ("amount", "velocity", 1), ("amount", "merchant", 1), ("merchant", "amount", 0.5),
                ("merchant", "velocity", 0.5)
            });
        }

        [Fact]
        public void AveragedOneDependenceWithNoSupportedEstimatorDoesNotVisualise()
        {
            Action act = () => ModelDiagramRenderer.Render(Aode([0, 0, 0]));

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*dropped for lack of support*");
        }

        [Fact]
        public void AveragedOneDependenceWithTooManyFeaturesIsTooLarge()
        {
            Action act = () => ModelDiagramRenderer.Render(Aode([.. Enumerable.Repeat(1.0, 40)]),
                new ModelDiagramOptions { MaximumElements = 100_000 });

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*summary*");
        }

        [Fact]
        public void HiddenNaiveBayesDrawsColumnJAsAParentOfRowIWithTheRowIColumnJWeight()
        {
            var edges = Edges(Spec(Hidden([[0, 0.7, 0], [0, 0, 0], [0.2, 0.9, 0]]), 3)).Where(e => e.From != "Class");

            edges.Should().BeEquivalentTo(new (string, string, double?)[]
            {
                ("velocity", "amount", 0.7), ("amount", "merchant", 0.2), ("velocity", "merchant", 0.9)
            });
        }

        [Fact]
        public void HiddenNaiveBayesIgnoresTheDiagonal()
        {
            var edges = Edges(Spec(Hidden([[5, 0], [0, 5]]), 2)).Where(e => e.From != "Class");

            edges.Should().BeEmpty();
        }

        [Fact]
        public void HiddenNaiveBayesWithANonSquareMatrixIsMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(Hidden([[0, 1, 0], [1, 0, 0]]));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*square*");
        }

        [Fact]
        public void ALatentTreeHasLatentNodesFeatureNodesAndDependenceStrengthOnTheEdges()
        {
            var json = Latent([-1, 0, 1, 1, 0], [2, 2, 2, 2, 2], [2, 3, 4],
            [
                null!, [[0.7, 0.3], [0.1, 0.9]], [[0.5, 0.5], [0.5, 0.5]], [[0.8, 0.2], [0.3, 0.7]],
                [[0.6, 0.4], [0.4, 0.6]]
            ]);

            var spec = Spec(json, 3);

            spec.Nodes.Select(n => (n.Label, n.Role)).Should().Equal(("Class", GraphNodeRole.Class),
                ("Z1", GraphNodeRole.Latent),
                ("amount", GraphNodeRole.Feature), ("velocity", GraphNodeRole.Feature),
                ("merchant", GraphNodeRole.Feature));

            spec.Edges.Select(e => (e.From, e.To)).Should()
                .Equal(("n0", "n1"), ("n1", "n2"), ("n1", "n3"), ("n0", "n4"));

            spec.Edges.Select(e => e.Weight!.Value).Should().BeEquivalentTo([0.6, 0.0, 0.5, 0.2],
                o => o.WithStrictOrdering().Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-9))
                    .WhenTypeIs<double>());
        }

        [Fact]
        public void ALatentTreeParentThatComesLaterIsMalformed()
        {
            var json = Latent([-1, 2, 0], [2, 2, 2], [1, 2],
                [null!, [[0.5, 0.5], [0.5, 0.5]], [[0.5, 0.5], [0.5, 0.5]]]);

            Action act = () => ModelDiagramRenderer.Render(json);

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*lower index*");
        }

        [Fact]
        public void TheRealLatentTreeFixtureNamesEveryFeatureAndAtLeastOneHiddenVariable()
        {
            var model = ModelDiagramFixtures.Parsed("bayes-latent-tree");
            var featureNodes = model["featureNodes"]!.Select(f => (int)f).ToArray();
            var featureNames = Enumerable.Range(0, featureNodes.Length).Select(i => "feat" + i).ToArray();

            var spec = ((GraphDiagram)ModelDiagramRenderer.Build(model.ToString(),
                new ModelDiagramOptions { VariableNames = featureNames })).Spec;

            spec.Nodes.Where(n => n.Role == GraphNodeRole.Feature).Select(n => n.Label).Should()
                .BeEquivalentTo(featureNames);

            spec.Nodes.Count(n => n.Role == GraphNodeRole.Latent).Should().BeGreaterThan(0);
            spec.Edges.Should().HaveCount(spec.Nodes.Count - 1);
            var parents = model["parents"]!.Select(p => (int)p).ToArray();

            for (var i = 1; i < parents.Length; i++)
            {
                var child = i;
                spec.Edges.Should().Contain(e => e.From == "n" + parents[child] && e.To == "n" + child);
            }
        }

        [Fact]
        public void ABayesGraphBeyondTheGraphLimitIsTooLarge()
        {
            const int n = 200;

            var json = Tan([.. Enumerable.Range(0, n).Select(i => i == 0 ? -1 : i - 1)],
                [.. Enumerable.Range(0, n).Select(_ => new double[n])]);

            Action act = () => ModelDiagramRenderer.Render(json, new ModelDiagramOptions { MaximumElements = 100_000 });

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*nodes*maximum is 150*summary*");
        }
    }
}
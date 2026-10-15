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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Service.ModelDiagrams;
using Jube.Test.Charting;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Test.ModelDiagrams
{
    public class MarkovAndComponentsAdapterTests
    {
        private const string Chain =
            "{\"transitions\":[[0.9,0.1],[0.2,0.8]],\"probabilities\":[0.6,0.4],\"emissions\":[[0.7,0.3],[0.1,0.9]]}";

        private const string TwoClusters =
            "[{\"centroid\":[1.0,10.0],\"index\":0,\"proportion\":0.6},{\"centroid\":[3.0,10.0],\"index\":1,\"proportion\":0.4}]";

        private const string TwoComponents =
            "{\"components\":[{\"eigenvalue\":3.0,\"eigenvector\":[1.0,0.0]},{\"eigenvalue\":1.0,\"eigenvector\":[0.0,1.0]}]}";

        private static GraphDiagramSpec Graph(string json, ModelDiagramOptions? options = null)
        {
            return ((GraphDiagram)ModelDiagramRenderer.Build(json, options)).Spec;
        }

        private static ComponentsDiagramSpec Components(string json, ModelDiagramOptions? options = null)
        {
            return ((ComponentsDiagram)ModelDiagramRenderer.Build(json, options)).Spec;
        }

        [Fact]
        public void TransitionProbabilitiesLandOnTheRightEdges()
        {
            var spec = Graph(Chain);

            spec.Edges.Select(e => (e.From, e.To, e.Probability)).Should().BeEquivalentTo(
                new (string, string, double?)[]
                {
                    ("s0", "s0", 0.9), ("s0", "s1", 0.1), ("s1", "s0", 0.2), ("s1", "s1", 0.8)
                });
        }

        [Fact]
        public void StatesAreNamedFromTheOptionsAndCarryInitialAndEmissionDetail()
        {
            var spec = Graph(Chain, new ModelDiagramOptions { VariableNames = ["Normal", "Suspicious"] });

            spec.Nodes.Select(n => n.Label).Should().Equal("Normal", "Suspicious");
            spec.Nodes.Should().OnlyContain(n => n.Role == GraphNodeRole.State);
            spec.Nodes[0].Detail.Should().Contain("Initial probability 0.6").And.Contain("symbol 0 0.7");
            spec.Nodes[1].Detail.Should().Contain("Initial probability 0.4").And.Contain("symbol 1 0.9");
        }

        [Fact]
        public void LogProbabilitiesAreExponentiated()
        {
            var json = "{\"logTransitions\":[[" + Math.Log(0.9) + "," + Math.Log(0.1) + "],[" + Math.Log(0.2) + "," +
                       Math.Log(0.8) +
                       "]],\"logInitial\":[" + Math.Log(0.6) + "," + Math.Log(0.4) + "],\"emissions\":[[" +
                       Math.Log(0.7) + "," +
                       Math.Log(0.3) + "],[" + Math.Log(0.1) + "," + Math.Log(0.9) + "]]}";

            var spec = Graph(json);

            spec.Edges.Select(e => e.Probability!.Value).Should().BeEquivalentTo([0.9, 0.1, 0.2, 0.8],
                o => o.Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-9))
                    .WhenTypeIs<double>());
        }

        [Fact]
        public void HostileNamesInTheJsonAreEscaped()
        {
            var model = ModelDiagramFixtures.Parsed("hmm-normal");
            ModelDiagramFixtures.HostileRename(model, "<script>alert(1)</script>\"'&");

            var svg = ModelDiagramRenderer.Render(model.ToString());

            SvgAssert.IsSafe(svg, false);
            svg.Should().NotContain("<script>alert");
        }

        [Fact]
        public void ImpossibleTransitionsAreNotDrawn()
        {
            var json = Chain.Replace("[[0.9,0.1],[0.2,0.8]]", "[[1.0,0.0],[0.2,0.8]]");

            Graph(json).Edges.Should().NotContain(e => e.From == "s0" && e.To == "s1");
        }

        [Fact]
        public void NormalEmissionsAreDescribedByTheirMeanAndDeviation()
        {
            var json = "{\"transitions\":[[0.7,0.3],[0.4,0.6]],\"probabilities\":[0.6,0.4],\"emissions\":[" +
                       "{\"mean\":0.0,\"standardDeviation\":1.0},{\"mean\":3.0,\"variance\":4.0}]}";

            var spec = Graph(json);

            spec.Nodes[0].Detail.Should().Contain("normal, mean 0, deviation 1");
            spec.Nodes[1].Detail.Should().Contain("normal, mean 3, deviation 2");
            spec.Options.Subtitle.Should().Contain("normal emissions");
        }

        [Fact]
        public void ARowThatIsNotAProbabilityDistributionIsMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(Chain.Replace("[0.9,0.1]", "[0.9,0.5]"));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*probability distribution*");
        }

        [Fact]
        public void EmissionsThatDoNotMatchTheStatesAreMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(Chain.Replace("[[0.7,0.3],[0.1,0.9]]", "[[0.7,0.3]]"));

            act.Should().Throw<ModelDiagramNotRecognisedException>()
                .WithMessage("*one emission distribution per state*");
        }

        [Fact]
        public void TheRealDiscreteFixtureMatchesItsStoredLogTransitions()
        {
            var model = ModelDiagramFixtures.Parsed("hmm-discrete");
            var log = model["logTransitions"]!.Select(r => r.Select(v => (double)v).ToArray()).ToArray();

            var spec = Graph(model.ToString());

            foreach (var edge in spec.Edges)
            {
                var from = int.Parse(edge.From[1..]);
                var to = int.Parse(edge.To[1..]);
                edge.Probability.Should().BeApproximately(Math.Exp(log[from][to]), 1e-9);
            }

            spec.Edges.Should().HaveCount(9);
        }

        [Fact]
        public void ManyStatesBeyondTheGraphLimitAreTooLarge()
        {
            const int n = 160;

            var row = "[" + string.Join(",",
                Enumerable.Repeat((1.0 / n).ToString("R", CultureInfo.InvariantCulture), n)) + "]";

            var json = "{\"transitions\":[" + string.Join(",", Enumerable.Repeat(row, n)) + "],\"probabilities\":" +
                       row +
                       ",\"emissions\":[" + string.Join(",", Enumerable.Repeat("[0.5,0.5]", n)) + "]}";

            Action act = () =>
                ModelDiagramRenderer.Render(json, new ModelDiagramOptions { MaximumElements = 1_000_000 });

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*summary*");
        }

        [Fact]
        public void ClusterCentroidsAreStandardisedAcrossClustersWithTheirSizes()
        {
            var spec = Components(TwoClusters, new ModelDiagramOptions { VariableNames = ["amount", "flat"] });

            spec.Features.Should().Equal("amount", "flat");
            spec.Clusters.Select(c => c.Name).Should().Equal("Cluster 1", "Cluster 2");
            spec.Clusters.Select(c => c.Size).Should().Equal(0.6, 0.4);
            spec.Clusters[0].Centroid.Should().Equal(-1.0, 0.0);
            spec.Clusters[1].Centroid.Should().Equal(1.0, 0.0);
            spec.Components.Should().BeEmpty();
            spec.ValueName.Should().Contain("z-score");
        }

        [Fact]
        public void ClustersInAnObjectWrapperAreAlsoUnderstood()
        {
            var spec = Components("{\"clusters\":" + TwoClusters + "}");

            spec.Clusters.Should().HaveCount(2);
        }

        [Fact]
        public void CentroidsOfDifferentLengthsAreMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(TwoClusters.Replace("[3.0,10.0]", "[3.0]"));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*same dimension*");
        }

        [Fact]
        public void TheRealClusterFixtureHasItsStoredProportions()
        {
            var model = JArray.Parse(ModelDiagramFixtures.Read("kmeans"));

            var spec = Components(model.ToString());

            spec.Clusters.Select(c => c.Size).Should().Equal(model.Select(c => (double)c["proportion"]!));
        }

        [Fact]
        public void MoreClustersThanTheChartHoldsIsTooLarge()
        {
            var clusters = string.Join(",",
                Enumerable.Range(0, 45).Select(i => "{\"centroid\":[" + i + ".0,1.0],\"index\":" + i + "}"));

            Action act = () => ModelDiagramRenderer.Render("[" + clusters + "]");

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*clusters*maximum is 40*summary*");
        }

        [Fact]
        public void ExplainedVarianceIsTheEigenvalueShareWhenNoProportionIsStored()
        {
            var spec = Components(TwoComponents, new ModelDiagramOptions { VariableNames = ["amount", "velocity"] });

            spec.Components.Select(c => (c.Name, c.ExplainedVariance)).Should().Equal(("PC1", 0.75), ("PC2", 0.25));
            spec.Components[0].Loadings.Should().Equal(1.0, 0.0);
            spec.Components[1].Loadings.Should().Equal(0.0, 1.0);
            spec.Features.Should().Equal("amount", "velocity");
            spec.Clusters.Should().BeEmpty();
        }

        [Fact]
        public void AStoredProportionIsPreferred()
        {
            var json = TwoComponents.Replace("\"eigenvalue\":3.0", "\"proportion\":0.5,\"eigenvalue\":3.0");

            Components(json).Components[0].ExplainedVariance.Should().Be(0.5);
        }

        [Fact]
        public void TheRealPcaFixtureUsesItsStoredEigenvectorsAsLoadings()
        {
            var model = ModelDiagramFixtures.Parsed("pca");

            var spec = Components(model.ToString());

            spec.Components.Should().HaveCount(4);
            spec.Components[0].Loadings.Should().Equal(model["components"]![0]!["eigenvector"]!.Select(v => (double)v));
            spec.Components.Sum(c => c.ExplainedVariance).Should().BeApproximately(1.0, 1e-6);
        }

        [Fact]
        public void EigenvectorsOfDifferentLengthsAreMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(TwoComponents.Replace("[0.0,1.0]", "[1.0]"));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*one length*");
        }

        [Fact]
        public void MoreFeaturesThanTheChartHoldsIsTooLarge()
        {
            var vector = "[" + string.Join(",", Enumerable.Repeat("0.1", 250)) + "]";

            Action act = () =>
                ModelDiagramRenderer.Render("{\"components\":[{\"eigenvalue\":1.0,\"eigenvector\":" + vector + "}]}");

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*features*maximum is 200*summary*");
        }
    }
}
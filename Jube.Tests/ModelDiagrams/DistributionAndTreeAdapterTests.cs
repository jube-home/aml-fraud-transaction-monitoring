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
using Jube.Test.Charting;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Test.ModelDiagrams
{
    public class DistributionAndTreeAdapterTests
    {
        private const string TwoByTwo =
            "{\"priors\":[0.3,0.7],\"distributions\":[[{\"mean\":0.0,\"standardDeviation\":1.0},{\"mean\":5.0,\"variance\":4.0}]," +
            "[{\"mean\":2.0,\"standardDeviation\":0.5},{\"mean\":6.0,\"variance\":9.0}]]}";

        private const string SmallTree =
            "{\"attributes\":[{\"name\":\"amount\"},{\"name\":\"velocity\"}],\"outputClasses\":2,\"classNames\":[\"genuine\",\"fraud\"]," +
            "\"root\":{\"attributeIndex\":0,\"branches\":[" +
            "{\"comparison\":\"LessThanOrEqual\",\"value\":5.0,\"attributeIndex\":1,\"branches\":[" +
            "{\"comparison\":\"LessThanOrEqual\",\"value\":2.0,\"output\":0},{\"comparison\":\"GreaterThan\",\"value\":2.0,\"output\":1}]}," +
            "{\"comparison\":\"GreaterThan\",\"value\":5.0,\"output\":1}]}}";

        private static DistributionPanelsSpec Panels(string json, ModelDiagramOptions? options = null)
        {
            return ((DistributionPanels)ModelDiagramRenderer.Build(json, options)).Spec;
        }

        private static TreeDiagramSpec Tree(string json, ModelDiagramOptions? options = null)
        {
            return ((TreeDiagram)ModelDiagramRenderer.Build(json, options)).Spec;
        }

        private static IEnumerable<double> Thresholds(TreeNode node)
        {
            if (node.Threshold is { } t)
            {
                yield return t;
            }

            foreach (var child in node.Branches.SelectMany(b => Thresholds(b.Node)))
            {
                yield return child;
            }
        }

        private static string Forest(int trees)
        {
            var tree =
                "{\"root\":{\"attributeIndex\":0,\"branches\":[{\"comparison\":\"LessThanOrEqual\",\"value\":1.0,\"output\":0}," +
                "{\"comparison\":\"GreaterThan\",\"value\":1.0,\"attributeIndex\":1,\"branches\":[" +
                "{\"comparison\":\"LessThanOrEqual\",\"value\":3.0,\"output\":0},{\"comparison\":\"GreaterThan\",\"value\":3.0,\"output\":1}]}]}}";

            return
                "{\"attributes\":[{\"name\":\"amount\"},{\"name\":\"velocity\"}],\"classNames\":[\"genuine\",\"fraud\"],\"trees\":[" +
                string.Join(",", Enumerable.Repeat(tree, trees)) + "]}";
        }

        [Fact]
        public void GaussianNaiveBayesMakesOnePanelPerFeatureWithOneCurvePerClass()
        {
            var spec = Panels(TwoByTwo, new ModelDiagramOptions { VariableNames = ["amount", "velocity"] });

            spec.Panels.Select(p => p.Title).Should().Equal("amount", "velocity");
            spec.Panels.Should().OnlyContain(p => p.Series.Count == 2);
            spec.Panels[0].Series.Select(s => s.Name).Should().Equal("Class 0 (prior 0.3)", "Class 1 (prior 0.7)");
        }

        [Fact]
        public void EachCurveIsCentredOnItsMeanWithItsDeviation()
        {
            var spec = Panels(TwoByTwo);

            var series = spec.Panels[0].Series[1];
            var middle = series.X.Count / 2;
            series.X[middle].Should().BeApproximately(2.0, 1e-9);
            series.Y[middle].Should().BeApproximately(1 / (0.5 * Math.Sqrt(2 * Math.PI)), 1e-9);
            var wide = spec.Panels[1].Series[1];
            wide.X[wide.X.Count / 2].Should().BeApproximately(6.0, 1e-9);
            wide.Y[wide.Y.Count / 2].Should().BeApproximately(1 / (3.0 * Math.Sqrt(2 * Math.PI)), 1e-9);
        }

        [Fact]
        public void AZeroVarianceFeatureDoesNotVisualise()
        {
            Action act = () =>
                ModelDiagramRenderer.Render(TwoByTwo.Replace("\"standardDeviation\":1.0", "\"standardDeviation\":0.0"));

            act.Should().Throw<ModelDiagramNotSupportedException>();
        }

        [Fact]
        public void PriorsThatDoNotMatchTheClassesAreMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(TwoByTwo.Replace("[0.3,0.7]", "[1.0]"));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*same classes*");
        }

        [Fact]
        public void MoreFeaturesThanPanelsIsTooLarge()
        {
            var feature = "{\"mean\":0.0,\"standardDeviation\":1.0}";
            var row = "[" + string.Join(",", Enumerable.Repeat(feature, 30)) + "]";

            Action act = () =>
                ModelDiagramRenderer.Render("{\"priors\":[0.5,0.5],\"distributions\":[" + row + "," + row + "]}");

            act.Should().Throw<ModelDiagramNotSupportedException>()
                .WithMessage("*distribution panels*maximum is 24*summary*");
        }

        [Fact]
        public void ThresholdsSitOnTheNodesThatSplitOnThem()
        {
            var root = Tree(SmallTree).Root!;

            root.Feature.Should().Be("amount");
            root.Threshold.Should().Be(5.0);
            var inner = root.Branches[0].Node;
            inner.Feature.Should().Be("velocity");
            inner.Threshold.Should().Be(2.0);
            inner.Branches[0].Node.Prediction.Should().Be("genuine");
            inner.Branches[1].Node.Prediction.Should().Be("fraud");
            root.Branches[1].Node.Prediction.Should().Be("fraud");
            root.Branches[1].Node.IsLeaf.Should().BeTrue();
        }

        [Fact]
        public void HostileAttributeAndClassNamesInTheJsonAreEscaped()
        {
            var model = ModelDiagramFixtures.Parsed("tree");
            ModelDiagramFixtures.HostileRename(model, "<script>alert(1)</script>\"'&");

            var svg = ModelDiagramRenderer.Render(model.ToString());

            SvgAssert.IsSafe(svg, false);
            svg.Should().NotContain("<script>alert");
        }

        [Fact]
        public void TheLessThanSideIsTheYesBranch()
        {
            var root = Tree(SmallTree).Root!;

            root.Branches.Select(b => b.Label).Should().Equal("yes", "no");
        }

        [Fact]
        public void ReversedBranchOrderStillPutsTheLessThanSideOnYes()
        {
            var model = JObject.Parse(SmallTree);
            var branches = (JArray)model["root"]!["branches"]!;
            var first = branches[0];
            branches.RemoveAt(0);
            branches.Add(first);

            var root = Tree(model.ToString()).Root!;

            root.Branches[0].Node.Feature.Should().Be("velocity");
            root.Branches[1].Node.Prediction.Should().Be("fraud");
        }

        [Fact]
        public void StrictLessThanIsShownAsAStrictCondition()
        {
            var json = SmallTree.Replace("\"LessThanOrEqual\",\"value\":5.0", "\"LessThan\",\"value\":5.0")
                .Replace("\"GreaterThan\",\"value\":5.0", "\"GreaterThanOrEqual\",\"value\":5.0");

            Tree(json).Root!.Condition.Should().Be("amount < 5");
        }

        [Fact]
        public void NumericComparisonCodesAreUnderstood()
        {
            var json = SmallTree.Replace("\"LessThanOrEqual\"", "6").Replace("\"GreaterThan\"", "4");

            var root = Tree(json).Root!;

            root.Threshold.Should().Be(5.0);
            root.Branches[0].Node.Feature.Should().Be("velocity");
        }

        [Fact]
        public void EqualityBranchesBecomeLabelledMultiwaySplits()
        {
            const string json =
                "{\"attributes\":[{\"name\":\"channel\"}],\"root\":{\"attributeIndex\":0,\"branches\":[" +
                "{\"comparison\":\"Equal\",\"value\":0,\"output\":0},{\"comparison\":\"Equal\",\"value\":1,\"output\":1}," +
                "{\"comparison\":\"Equal\",\"value\":2,\"output\":0}]}}";

            var root = Tree(json).Root!;

            root.Feature.Should().Be("channel");
            root.Branches.Select(b => b.Label).Should().Equal("= 0", "= 1", "= 2");
        }

        [Fact]
        public void VariableNamesOverrideTheStoredAttributeNames()
        {
            var root = Tree(SmallTree, new ModelDiagramOptions { VariableNames = ["a", "b"] }).Root!;

            root.Feature.Should().Be("a");
            root.Branches[0].Node.Feature.Should().Be("b");
        }

        [Fact]
        public void ClassNamesAreCarriedToTheDiagram()
        {
            Tree(SmallTree).ClassNames.Should().Equal("genuine", "fraud");
        }

        [Fact]
        public void ASplitOnAnAttributeThatDoesNotExistIsMalformed()
        {
            Action act = () =>
                ModelDiagramRenderer.Render(SmallTree.Replace("\"attributeIndex\":1", "\"attributeIndex\":9"));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*attribute 9*");
        }

        [Fact]
        public void ALeafWithoutAnOutputIsMalformed()
        {
            Action act = () => ModelDiagramRenderer.Render(SmallTree.Replace("\"output\":0", "\"x\":0"));

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*output class*");
        }

        [Fact]
        public void TheRealTreeFixtureKeepsEveryStoredThreshold()
        {
            var model = ModelDiagramFixtures.Parsed("tree");

            var expected = model["root"]!.SelectTokens("$..branches[0].value").Select(v => (double)v).OrderBy(v => v)
                .ToArray();

            var root = Tree(model.ToString(), new ModelDiagramOptions { VariableNames = ["a", "b", "c", "d"] }).Root!;

            Thresholds(root).OrderBy(v => v).Should().Equal(expected);
        }

        [Fact]
        public void AVeryLargeTreeBeyondTheElementCapDoesNotVisualise()
        {
            Action act = () => ModelDiagramRenderer.Render(SmallTree, new ModelDiagramOptions { MaximumElements = 3 });

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*5 tree nodes*maximum is 3*");
        }

        [Fact]
        public void ATreeDeeperThanTheChartTakesIsCollapsedNotRejected()
        {
            var leaf = "{\"comparison\":\"Equal\",\"value\":0,\"output\":0}";
            var json = leaf;

            for (var i = 0; i < 40; i++)
            {
                json = "{\"comparison\":\"Equal\",\"value\":0,\"attributeIndex\":0,\"branches\":[" + json + "," + leaf +
                       "]}";
            }

            var svg = ModelDiagramRenderer.Render("{\"attributes\":[{\"name\":\"x\"}],\"root\":" + json + "}");

            svg.Should().StartWith("<svg ");
        }

        [Fact]
        public void AForestShowsTheFirstTreesAndSplitShareImportances()
        {
            var spec = ((ForestOfTrees)ModelDiagramRenderer.Build(Forest(10))).Spec;

            spec.TotalTrees.Should().Be(10);
            spec.Trees.Should().HaveCount(6);
            spec.Trees[0].Root!.Feature.Should().Be("amount");
            spec.Importances.Select(i => (i.Name, i.Value)).Should().Equal(("amount", 0.5), ("velocity", 0.5));
            spec.ImportanceName.Should().Be("Share of splits");
        }

        [Fact]
        public void ImportancesCountEverySplitNotJustTheDrawnTrees()
        {
            var json = Forest(3).Replace("\"attributeIndex\":1", "\"attributeIndex\":0");

            var spec = ((ForestOfTrees)ModelDiagramRenderer.Build(json)).Spec;

            spec.Importances.Should().ContainSingle().Which.Name.Should().Be("amount");
        }

        [Fact]
        public void ALargeEnsembleBeyondTheCapDoesNotVisualise()
        {
            Action act = () =>
                ModelDiagramRenderer.Render(Forest(400), new ModelDiagramOptions { MaximumElements = 1000 });

            act.Should().Throw<ModelDiagramNotSupportedException>()
                .WithMessage("*forest nodes*maximum is 1000*summary*");
        }

        [Fact]
        public void AForestWithMoreThanEightClassesDoesNotVisualise()
        {
            var names = string.Join(",", Enumerable.Range(0, 9).Select(i => "\"c" + i + "\""));
            var json = Forest(2).Replace("[\"genuine\",\"fraud\"]", "[" + names + "]");

            Action act = () => ModelDiagramRenderer.Render(json);

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*8 classes*");
        }

        [Fact]
        public void TheRealForestFixtureDrawsEveryStoredTree()
        {
            var model = ModelDiagramFixtures.Parsed("forest");

            var spec = ((ForestOfTrees)ModelDiagramRenderer.Build(model.ToString())).Spec;

            spec.TotalTrees.Should().Be(model["trees"]!.Count());
            spec.Trees.Should().HaveCount(Math.Min(6, spec.TotalTrees));
            spec.Importances.Sum(i => i.Value).Should().BeApproximately(1.0, 1e-9);
        }
    }
}
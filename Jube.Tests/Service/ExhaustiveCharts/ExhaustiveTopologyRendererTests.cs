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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using FluentAssertions;
using Jube.Charting.Core.Models;
using Jube.Service.Charts;
using Jube.Test.ModelDiagrams;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class ExhaustiveTopologyRendererTests
    {
        private static string Json(string name)
        {
            return ModelDiagramFixtures.Read("topology:" + name);
        }

        private static List<string> Names(string json)
        {
            return [.. JObject.Parse(json)["annotations"]!.Select(a => a["name"]!.Value<string>()!)];
        }

        private static List<string> Texts(string svg)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg);
            document.GetElementsByTagName("script").Count.Should().Be(0);
            svg.Should().NotContainEquivalentOf("<script");
            svg.Should().NotContainEquivalentOf("foreignObject");
            svg.Should().NotContain("<style");
            svg.Should().NotContain("style=");
            svg.Should().NotContain("NaN");
            Regex.IsMatch(svg, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase).Should().BeFalse();
            document.DocumentElement!.GetAttribute("class").Split(' ').Should().Contain("jc");

            return
            [
                .. document.GetElementsByTagName("text").Cast<XmlNode>().Select(n => n.InnerText),
                .. document.GetElementsByTagName("title").Cast<XmlNode>().Select(n => n.InnerText)
            ];
        }

        private static string BigNetwork(int inputs)
        {
            var weights = string.Join(",", Enumerable.Repeat("0", inputs));

            return "{\"layers\":[{\"neurons\":[{\"weights\":[" + weights + "],\"threshold\":0}]}]}";
        }

        [Fact]
        public void ARealStoredTopologyIsDrawnWithItsVariableNamesAndACaption()
        {
            var json = Json("Sigmoid2");
            var text = Texts(ExhaustiveTopologyRenderer.Topology(json, Names(json), null, 6, 0.8123, id: "ex-tp-1"));

            text.Should().Contain(ExhaustiveTopologyRenderer.Title);
            text.Should().Contain("Abstraction.IsChip");

            text.Should().Contain(t =>
                t.StartsWith(
                    "26 inputs, 5 hidden neurons, 1 output; 6 PEs and 135 weights in 2 layers; activation Sigmoid") &&
                t.Contains("topology complexity 6") && t.Contains("score 0.812"));

            text.Should().Contain("Inputs");
            text.Should().Contain("Hidden 1");
            text.Should().Contain("Output");
            text.Should().NotContain(t => t.Contains("nodes aggregated"));
            text.Should().Contain(t => t.Contains("view: full diagram, weight labels dropped"));
        }

        [Theory]
        [InlineData("Sigmoid2")]
        [InlineData("BipolarSigmoid1")]
        [InlineData("Gaussian")]
        [InlineData("RectifiedLinear")]
        [InlineData("Linear1")]
        [InlineData("Identity")]
        public void EveryRealTopologyRendersSafeExternalSvgAndStatesItsActivation(string name)
        {
            var json = Json(name);
            var svg = ExhaustiveTopologyRenderer.Topology(json, Names(json), null, 6, 0.5, id: "ex-tp-2");

            var text = Texts(svg);

            text.Should().Contain(t => t.Contains("activation "));
            svg.Should().NotContain("<style");
            ExhaustiveTopologyRenderer.Topology(json, Names(json), null, 6, 0.5, id: "ex-tp-2").Should().Be(svg);
        }

        [Fact]
        public void InputNodesAreShadedBySensitivityWithAKeyAndHoverDetail()
        {
            var json = Json("Sigmoid2");
            var names = Names(json);

            var sensitivities = new List<TopologyInputSensitivity>
            {
                new(names[0], 0.4), new(names[1], 0.1), new(names[2], 0.2)
            };

            var svg = ExhaustiveTopologyRenderer.Topology(json, names, sensitivities, 6, 0.5, id: "ex-tp-3");
            var text = Texts(svg);

            text.Should().Contain(ExhaustiveTopologyRenderer.SensitivityTitle);
            text.Should().Contain("low");
            text.Should().Contain("high");
            text.Should().Contain(t => t.Contains("Input sensitivity: 0.4"));
            text.Should().Contain(t => t.Contains("Input sensitivity: 0.1"));
            svg.Should().Contain("nq8");
            svg.Should().Contain("nq2");
        }

        [Fact]
        public void LiveRelevanceShadesEveryLayerNotJustInputsAndTakesPrecedenceOverSensitivity()
        {
            var json = "{\"layers\":[{\"neurons\":[{\"weights\":[1,-2],\"threshold\":0}," +
                       "{\"weights\":[0.5,0.5],\"threshold\":0}]},{\"neurons\":[{\"weights\":[1,1],\"threshold\":0}]}]}";

            var sensitivities = new List<TopologyInputSensitivity> { new("0", 1), new("1", 1) };
            List<IReadOnlyList<double>> liveRelevance = [[0.8, -0.2], [0.1, 0.4], [0.5]];

            var svg = ExhaustiveTopologyRenderer.Topology(json, null, sensitivities, liveRelevance: liveRelevance,
                id: "ex-tp-live");

            var text = Texts(svg);

            text.Should().Contain(ExhaustiveTopologyRenderer.RelevanceTitle);
            text.Should().NotContain(ExhaustiveTopologyRenderer.SensitivityTitle);
            text.Should().Contain(t => t.Contains("+0.8"));
            text.Should().Contain(t => t.Contains("-0.2"));
            text.Should().Contain(t => t.Contains("+0.5"));
            svg.Should().Contain("nq8");
        }

        [Fact]
        public void LiveRelevanceWithAMismatchedLayerCountIsIgnored()
        {
            var json = "{\"layers\":[{\"neurons\":[{\"weights\":[1,-2],\"threshold\":0}]}]}";
            List<IReadOnlyList<double>> tooFew = [[0.8, -0.2]];

            var svg = ExhaustiveTopologyRenderer.Topology(json, liveRelevance: tooFew, id: "ex-tp-live-2");

            svg.Should().NotContain("nq");
            Texts(svg).Should().NotContain(ExhaustiveTopologyRenderer.RelevanceTitle);
        }

        [Fact]
        public void WithoutSensitivityTheDiagramHasNoKeyAndNoShading()
        {
            var json = Json("Sigmoid2");

            var svg = ExhaustiveTopologyRenderer.Topology(json, Names(json), null, 6, 0.5, id: "ex-tp-4");

            svg.Should().NotContain("nq");
            Texts(svg).Should().NotContain(ExhaustiveTopologyRenderer.SensitivityTitle);
        }

        [Fact]
        public void SensitivityOnlyForUnknownNamesOrAllZeroDoesNotShadeAnything()
        {
            var json = Json("Sigmoid2");
            var names = Names(json);

            var unknown = ExhaustiveTopologyRenderer.Topology(json, names,
                [new TopologyInputSensitivity("Nope", 1)], 6, 0.5, id: "ex-tp-5");

            var zero = ExhaustiveTopologyRenderer.Topology(json, names,
                [new TopologyInputSensitivity(names[0], 0), new TopologyInputSensitivity(names[1], double.NaN)], 6,
                0.5, id: "ex-tp-5");

            unknown.Should().NotContain("nq");
            zero.Should().NotContain("nq");
        }

        [Fact]
        public void ANameCountThatDoesNotMatchTheInputsFallsBackToTheStoredNamesInsteadOfFailing()
        {
            var json = Json("Sigmoid2");

            var text = Texts(ExhaustiveTopologyRenderer.Topology(json, ["only", "three", "names"], null, 6, 0.5,
                id: "ex-tp-6"));

            text.Should().Contain(t => t.StartsWith("26 inputs"));
            text.Should().NotContain("only");
        }

        [Fact]
        public void HostileVariableNamesAndSensitivityNamesCannotInjectMarkup()
        {
            var json = Json("Sigmoid2");
            var hostile = Enumerable.Range(0, 26).Select(i => i == 0 ? "<script>alert(1)</script>" : "v" + i).ToList();

            var svg = ExhaustiveTopologyRenderer.Topology(json, hostile,
                [new TopologyInputSensitivity("<script>alert(1)</script>", 3)], 6, 0.5, id: "ex-tp-7");

            Texts(svg).Should().NotBeEmpty();
            svg.Should().NotContain("<script>alert");
        }

        [Fact]
        public void NoJsonGivesTheNotTrainedEmptyState()
        {
            foreach (var json in new[] { null, "", "   " })
            {
                var text = Texts(ExhaustiveTopologyRenderer.Topology(json));

                text.Should().Contain(ExhaustiveTopologyRenderer.Title);
                text.Should().Contain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
                text.Should().Contain(t => t.Contains("has not been trained yet"));
            }
        }

        [Theory]
        [InlineData("not json at all")]
        [InlineData("{}")]
        [InlineData("[1,2,3]")]
        [InlineData("{\"layers\":\"nope\"}")]
        [InlineData(
            "{\"layers\":[{\"neurons\":[{\"weights\":[1,2],\"threshold\":0},{\"weights\":[1],\"threshold\":0}]}]}")]
        public void UnreadableJsonGivesAClearMessageInsteadOfAnError(string json)
        {
            var text = Texts(ExhaustiveTopologyRenderer.Topology(json));

            text.Should().Contain(ExhaustiveTopologyRenderer.Title);

            text.Should().Contain(t => t == ExhaustiveTopologyRenderer.UnreadableMessage ||
                                       t == ExhaustiveTopologyRenderer.TooLargeMessage);
        }

        [Fact]
        public void AVeryWideNetworkIsDrawnAsTheDensityViewNotAnError()
        {
            var text = Texts(ExhaustiveTopologyRenderer.Topology(BigNetwork(6000), null, null, 1, 0.5, id: "ex-tp-8"));

            text.Should().Contain("6000 inputs");
            text.Should().Contain(t => t.Contains("view: density view"));
            text.Should().Contain("Weight magnitude between layers, rows are the earlier layer and columns the next");
        }

        [Fact]
        public void ANetworkBeyondTheLayerLimitOffersTheLayerSummaryInsteadOfADiagram()
        {
            var neurons = "{\"weights\":[0,0],\"threshold\":0}";

            var layers = string.Join(",",
                Enumerable.Range(0, 17).Select(_ => "{\"neurons\":[" + neurons + "," + neurons + "]}"));

            var svg = ExhaustiveTopologyRenderer.Topology("{\"layers\":[" + layers + "]}", null, null, 34, 0.5,
                id: "ex-tp-8");

            var text = Texts(svg);

            text.Should().Contain(ExhaustiveTopologyRenderer.TooLargeMessage);
            text.Should().Contain("Inputs");
            text.Should().Contain("Output");
            text.Should().Contain("Layers");
            text.Should().Contain("17");
            text.Should().Contain("Topology complexity");
        }

        [Fact]
        public void AnyExceptionMessageFromTheStoredJsonNeverAppearsInTheOutput()
        {
            var svg = ExhaustiveTopologyRenderer.Topology(
                "{\"secret\":\"SECRET-TOKEN\",\"layers\":[{\"neurons\":[{}]}]}");

            svg.Should().NotContain("SECRET-TOKEN");
        }

        [Fact]
        public void InlineModeStillWorks()
        {
            var json = Json("Sigmoid2");

            ExhaustiveTopologyRenderer.Topology(json, Names(json), null, 6, 0.5, ChartStyleMode.Inline, "ex-tp-9")
                .Should().Contain("<style");
        }

        [Fact]
        public void ATwoTwoOneNetworkIsDrawnInFull()
        {
            var json =
                "{\"layers\":[{\"neurons\":[{\"weights\":[0.5,-0.25],\"threshold\":0.1,\"activationFunction\":{\"$type\":\"SigmoidFunction\",\"alpha\":2}}," +
                "{\"weights\":[-1,1],\"threshold\":-0.2,\"activationFunction\":{\"$type\":\"SigmoidFunction\",\"alpha\":2}}]}," +
                "{\"neurons\":[{\"weights\":[1.5,-2],\"threshold\":0,\"activationFunction\":{\"$type\":\"SigmoidFunction\",\"alpha\":2}}]}]}";

            var text = Texts(ExhaustiveTopologyRenderer.Topology(json, ["amount", "velocity"],
                [new TopologyInputSensitivity("amount", 0.9), new TopologyInputSensitivity("velocity", 0.3)], 3, 0.9,
                id: "ex-tp-10"));

            text.Should().Contain("amount");
            text.Should().Contain("velocity");

            text.Should().Contain(t =>
                t.StartsWith(
                    "2 inputs, 2 hidden neurons, 1 output; 3 PEs and 6 weights in 2 layers; activation Sigmoid, alpha 2"));
            text.Should().Contain(t => t.Contains("Input sensitivity: 0.9"));
            text.Should().Contain(t => t.Contains("Input sensitivity: 0.3"));
            text.Should().NotContain(t => t.Contains("nodes aggregated"));
        }
    }
}
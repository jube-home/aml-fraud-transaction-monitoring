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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Charting.Core.Models;
using Jube.Service.ModelDiagrams;
using Jube.Test.Charting;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Test.ModelDiagrams
{
    public class ModelDiagramRenderingTests
    {
        private static readonly string[] hostile =
        [
            "<script>alert(1)</script>",
            "\"onload=\"alert(1)\" x=\"",
            "a & b < c > d ' \" ]]> <![CDATA[",
            "javascript:alert(1)",
            "http://evil.example/x",
            "url(http://evil.example)",
            "&#x3c;script&#x3e;",
            "‮evil‬",
            "line\nbreak\u0000nul",
            "😀 emoji",
            "@import 'x'"
        ];

        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.EveryFixtureData), MemberType = typeof(ModelDiagramFixtures))]
        public void EveryFixtureRendersOneWellFormedSafeSvg(string name)
        {
            var svg = ModelDiagramRenderer.Render(ModelDiagramFixtures.Read(name));

            SvgAssert.IsSafe(svg);
            svg.Should().StartWith("<svg ").And.EndWith("</svg>");
            svg.Should().NotContain("<script").And.NotContain("href").And.NotContain("xlink");
            svg.Split("<svg ").Length.Should().Be(2);
        }

        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.EveryFixtureData), MemberType = typeof(ModelDiagramFixtures))]
        public void EveryFixtureRendersIdenticalBytesEveryTime(string name)
        {
            var json = ModelDiagramFixtures.Read(name);

            var first = ModelDiagramRenderer.Render(json);

            ModelDiagramRenderer.Render(json).Should().Be(first);
            ModelDiagramRenderer.Render(json, new ModelDiagramOptions()).Should().Be(first);
        }

        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.EveryFixtureData), MemberType = typeof(ModelDiagramFixtures))]
        public void EveryFixtureCarriesHoverDetailUnlessAskedNotTo(string name)
        {
            var json = ModelDiagramFixtures.Read(name);

            var interactive = ModelDiagramRenderer.Render(json);
            var plain = ModelDiagramRenderer.Render(json, new ModelDiagramOptions { Interactive = false });

            interactive.Should().Contain("tabindex=\"0\"").And.Contain("class=\"tt\"");
            plain.Should().NotContain("tabindex=").And.NotContain("class=\"tt\"");
            SvgAssert.IsSafe(plain);
        }

        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.EveryFixtureData), MemberType = typeof(ModelDiagramFixtures))]
        public void TheTitleOverrideIsUsed(string name)
        {
            var svg = ModelDiagramRenderer.Render(ModelDiagramFixtures.Read(name),
                new ModelDiagramOptions { Title = "Custom title for review" });

            svg.Should().Contain("Custom title for review");
        }

        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.EveryFixtureData), MemberType = typeof(ModelDiagramFixtures))]
        public void TheThemeOptionSelectsTheDataTheme(string name)
        {
            var json = ModelDiagramFixtures.Read(name);

            ModelDiagramRenderer.Render(json, new ModelDiagramOptions { Theme = ThemeMode.Dark }).Should()
                .Contain("data-theme=\"dark\"");
            ModelDiagramRenderer.Render(json, new ModelDiagramOptions { Theme = ThemeMode.Light }).Should()
                .Contain("data-theme=\"light\"");
        }

        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.EveryFixtureData), MemberType = typeof(ModelDiagramFixtures))]
        public void TheSizeOptionSetsTheMinimumCanvas(string name)
        {
            var json = ModelDiagramFixtures.Read(name);

            var small = ModelDiagramRenderer.Render(json, new ModelDiagramOptions { Width = 500, Height = 300 });
            var large = ModelDiagramRenderer.Render(json, new ModelDiagramOptions { Width = 1400, Height = 900 });

            small.Should().NotBe(large);
            SvgAssert.IsSafe(small);
            SvgAssert.IsSafe(large);
        }

        [Theory]
        [InlineData(10, 400)]
        [InlineData(5000, 400)]
        [InlineData(400, 5)]
        [InlineData(400, 9000)]
        [InlineData(double.NaN, 400)]
        public void ASizeOutsideTheChartRangeIsRejected(double width, double height)
        {
            Action act = () => ModelDiagramRenderer.Render(ModelDiagramFixtures.Read("bayes-tan"),
                new ModelDiagramOptions { Width = width, Height = height });

            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.StoredData), MemberType = typeof(ModelDiagramFixtures))]
        public void HostileVariableNamesAreEscapedInEveryAdapter(string name)
        {
            var count = ModelDiagramFixtures.Stored.Single(c => c.Name == name).Variables;
            var names = Enumerable.Range(0, count).Select(i => hostile[i % hostile.Length]).ToArray();

            var svg = ModelDiagramRenderer.Render(ModelDiagramFixtures.Read(name),
                new ModelDiagramOptions { VariableNames = names });

            SvgAssert.IsSafe(svg, false);
            svg.Should().NotContain("<script").And.NotContain("onload=\"alert");
        }

        [Theory]
        [InlineData("topology:Sigmoid2", 26)]
        [InlineData("topology:Identity", 26)]
        public void HostileVariableNamesAreEscapedInTheNetwork(string name, int count)
        {
            var names = Enumerable.Range(0, count).Select(i => hostile[i % hostile.Length]).ToArray();

            var svg = ModelDiagramRenderer.Render(ModelDiagramFixtures.Read(name),
                new ModelDiagramOptions { VariableNames = names });

            SvgAssert.IsSafe(svg, false);
        }

        [Theory]
        [InlineData("topology:Sigmoid2")]
        [InlineData("snapshot-logistic-pipeline")]
        [InlineData("snapshot-logistic-pipeline-evidence")]
        public void HostileLabelsFromTheJsonAreEscaped(string name)
        {
            var model = ModelDiagramFixtures.Parsed(name);
            var hostileLabel = "<script>alert(1)</script>\"'&";
            ModelDiagramFixtures.HostileRename(model, hostileLabel);

            var svg = ModelDiagramRenderer.Render(model.ToString());

            SvgAssert.IsSafe(svg, false);
            svg.Should().NotContain("<script>alert");
        }

        [Fact]
        public void HostileEvidenceAndVariableNamesInASnapshotAreEscaped()
        {
            var model = ModelDiagramFixtures.Parsed("snapshot-logistic-pipeline-evidence");

            foreach (var row in (JArray)model["evidence"]!["coefficients"]!)
            {
                row["name"] = "<script>alert(1)</script>";
            }

            var svg = ModelDiagramRenderer.Render(model.ToString());

            SvgAssert.IsSafe(svg, false);
            svg.Should().NotContain("<script>alert");
        }

        [Fact]
        public void TheVariableNameOverrideMustMatchTheModel()
        {
            Action act = () => ModelDiagramRenderer.Render(ModelDiagramFixtures.Read("bayes-tan"),
                new ModelDiagramOptions { VariableNames = ["only", "two"] });

            act.Should().Throw<ArgumentException>().WithMessage("*2 names*4 inputs*");
        }

        [Fact]
        public void ManyThreadsRenderTheSameBytes()
        {
            var inputs = ModelDiagramFixtures.Stored.Select(c => c.Name)
                .Concat(ModelDiagramFixtures.TopologyNames.Select(n => "topology:" + n)).ToArray();

            var expected = inputs.ToDictionary(n => n, n => ModelDiagramRenderer.Render(ModelDiagramFixtures.Read(n)));

            var results = new ConcurrentBag<(string Name, string Svg)>();

            Parallel.For(0, 160, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
            {
                var name = inputs[i % inputs.Length];
                results.Add((name, ModelDiagramRenderer.Render(ModelDiagramFixtures.Read(name))));
            });

            results.Should().HaveCount(160);
            results.Should().OnlyContain(r => r.Svg == expected[r.Name]);
        }

        [Fact]
        public void DifferentOptionsOnDifferentThreadsDoNotLeak()
        {
            var json = ModelDiagramFixtures.Read("snapshot-logistic-pipeline");
            var results = new ConcurrentDictionary<int, string>();

            Parallel.For(0, 64, new ParallelOptions { MaxDegreeOfParallelism = 8 },
                i =>
                {
                    results[i] = ModelDiagramRenderer.Render(json, new ModelDiagramOptions { Title = "Run " + i });
                });

            for (var i = 0; i < 64; i++)
            {
                results[i].Should().Contain("Run " + i + "<").And.NotContain("Run " + (i + 1) + "<");
            }
        }

        [Fact]
        public void ANetworkBeyondTheElementCapIsNotSupported()
        {
            Action act = () => ModelDiagramRenderer.Render(ModelDiagramFixtures.Read("topology:Sigmoid2"),
                new ModelDiagramOptions { MaximumNetworkElements = 10 });

            act.Should().Throw<ModelDiagramNotSupportedException>()
                .WithMessage("*32 network nodes*maximum is 10*summary*");
        }
    }
}
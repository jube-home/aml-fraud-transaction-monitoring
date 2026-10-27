/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Charting.Models;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.Export.Interfaces;
using Jube.AskJooby.Tools.Charting;
using Jube.Service.AskJoobyCharting;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class RenderChartToolTests
    {
        private static CachedTable BuildTable()
        {
            return new CachedTable(
                [
                    new CachedColumn("RuleName", "string"), new CachedColumn("Hits", "double"),
                    new CachedColumn("Blocked", "double")
                ],
                [
                    new Dictionary<string, object?> { ["RuleName"] = "Velocity", ["Hits"] = 120.0, ["Blocked"] = 12.0 },
                    new Dictionary<string, object?> { ["RuleName"] = "Geo", ["Hits"] = 80.0, ["Blocked"] = 31.0 },
                    new Dictionary<string, object?> { ["RuleName"] = "Mule", ["Hits"] = 45.0, ["Blocked"] = 45.0 }
                ]);
        }

        private static RenderChartTool BuildTool(FakeExport export, CachedTable? table = null)
        {
            return new RenderChartTool(new FakeCache(table ?? BuildTable()), new AskJoobyChartRenderer(),
                export, export);
        }

        [Fact]
        public async Task RenderChartNeverReturnsTheChartItselfOnlyThatItWasDrawnAsync()
        {
            var export = new FakeExport();
            var tool = BuildTool(export);

            var result = await tool.RenderChart("tbl_x", "column", "RuleName", "Hits", "Hits by rule");

            result.Should().NotContain("<svg",
                "the entire point is that the picture reaches the page without passing through the "
                + "model, so a tool that hands the markup back has achieved nothing");

            result.Should().NotContain("Velocity",
                "nor should the data it was drawn from come back, for the same reason");

            result.Should().Contain("3 row(s)");
        }

        [Fact]
        public async Task RenderChartPutsRealSvgOntoTheReplyAsAnAttachmentAsync()
        {
            var export = new FakeExport();
            var tool = BuildTool(export);

            await tool.RenderChart("tbl_x", "column", "RuleName", "Hits", "Hits by rule");

            export.Files.Should().HaveCount(1);
            export.ContentTypes.Single().Should().Be("image/svg+xml");

            export.Files.Single().Name.Should().EndWith(".svg",
                "the chat renders an attachment inline only when it can tell from the name that it "
                + "is a drawing rather than a download");

            var svg = Encoding.UTF8.GetString(export.Bytes.Single());
            svg.Should().StartWith("<svg").And.EndWith("</svg>");

            svg.Should().Contain("Velocity", "the chart is drawn from the cached rows, not from anything "
                                             + "the model wrote");
        }

        [Fact]
        public async Task RenderChartDrawsSeveralSeriesWhenGivenSeveralValueColumnsAsync()
        {
            var export = new FakeExport();
            var tool = BuildTool(export);

            await tool.RenderChart("tbl_x", "column", "RuleName", "Hits, Blocked");

            var svg = Encoding.UTF8.GetString(export.Bytes.Single());
            svg.Should().Contain("Hits").And.Contain("Blocked");
        }

        [Fact]
        public async Task RenderChartReportsAMissingPointerRatherThanThrowingAsync()
        {
            var export = new FakeExport();
            var tool = new RenderChartTool(new FakeCache(null), new AskJoobyChartRenderer(), export, export);

            var result = await tool.RenderChart("tbl_gone", "column", "RuleName", "Hits");

            result.Should().Contain("No cached table matches that pointer");
            export.Files.Should().BeEmpty();
        }

        [Fact]
        public async Task RenderChartNamesTheRealColumnsWhenGivenOneThatDoesNotExistAsync()
        {
            var export = new FakeExport();
            var tool = BuildTool(export);

            var result = await tool.RenderChart("tbl_x", "column", "NoSuchColumn", "Hits");

            result.Should().Contain("Could not draw that chart");

            result.Should().Contain("RuleName",
                "a model that guessed a column name can only correct itself if it is told the real ones");

            export.Files.Should().BeEmpty();
        }

        [Fact]
        public async Task RenderChartListsTheTypesItKnowsWhenGivenOneItDoesNotAsync()
        {
            var export = new FakeExport();
            var tool = BuildTool(export);

            var result = await tool.RenderChart("tbl_x", "sankey", "RuleName", "Hits");

            result.Should().Contain("not a chart type");
            result.Should().Contain("column").And.Contain("pie");
            export.Files.Should().BeEmpty();
        }

        [Fact]
        public async Task RenderChartRefusesAColumnThatHoldsNothingNumericAsync()
        {
            var export = new FakeExport();
            var tool = BuildTool(export);

            var result = await tool.RenderChart("tbl_x", "column", "Hits", "RuleName");

            result.Should().Contain("could be read as a number",
                "plotting a text column silently as a row of zeroes is worse than refusing it");

            export.Files.Should().BeEmpty();
        }

        [Theory]
        [InlineData("column")]
        [InlineData("bar")]
        [InlineData("line")]
        [InlineData("area")]
        [InlineData("pie")]
        [InlineData("donut")]
        public async Task EveryCategoricalChartTypeTheToolAdvertisesActuallyDrawsAsync(string chartType)
        {
            var export = new FakeExport();
            var tool = BuildTool(export);

            var result = await tool.RenderChart("tbl_x", chartType, "RuleName", "Hits");

            result.Should().NotContain("Could not draw",
                "a type named in the tool description that fails on a plain table is a type the model "
                + "will keep choosing and keep failing with");

            Encoding.UTF8.GetString(export.Bytes.Single()).Should().StartWith("<svg");
        }

        [Fact]
        public async Task ScatterPlotsOneNumericColumnAgainstAnotherAsync()
        {
            var export = new FakeExport();
            var tool = BuildTool(export);

            var result = await tool.RenderChart("tbl_x", "scatter", "Hits", "Blocked");

            result.Should().NotContain("Could not draw");
            Encoding.UTF8.GetString(export.Bytes.Single()).Should().StartWith("<svg");
        }

        [Fact]
        public async Task RenderChartRefusesATableTooLargeToReadAsAChartAsync()
        {
            var rows = new List<IReadOnlyDictionary<string, object?>>();

            for (var i = 0; i < 2500; i++)
            {
                rows.Add(new Dictionary<string, object?> { ["RuleName"] = "r" + i, ["Hits"] = (double)i });
            }

            var export = new FakeExport();

            var tool = BuildTool(export, new CachedTable(
                [new CachedColumn("RuleName", "string"), new CachedColumn("Hits", "double")], rows));

            var result = await tool.RenderChart("tbl_x", "column", "RuleName", "Hits");

            result.Should().Contain("Could not draw that chart");

            result.Should().Contain("Aggregate it in SQL",
                "one bar per row means a chart of raw rows is unreadable, and the fix is upstream "
                + "in the query rather than anything the chart can do about it");
        }

        private static readonly string[] OriginalEight =
            ["column", "bar", "line", "area", "pie", "donut", "scatter", "histogram"];

        private static readonly string[] RegistryKinds =
        [
            "stackedcolumn", "stackedbar", "percentstackedcolumn", "percentstackedbar", "stackedarea",
            "percentstackedarea", "heatmap", "combo", "boxplot", "candlestick", "waterfall", "funnel", "radar",
            "polar", "gauge", "bullet", "sparkline", "treemap", "gantt", "map"
        ];

        [Fact]
        public void TheRendererAdvertisesExactlyTheTypesTheToolDescriptionNames()
        {
            var advertised = new AskJoobyChartRenderer().SupportedChartTypes;

            advertised.Should().BeEquivalentTo([.. OriginalEight, .. RegistryKinds],
                "the description the model reads and the list the renderer accepts have to be the "
                + "same list, or it will be told to choose something that cannot be drawn");

            var description = typeof(RenderChartTool).GetMethod(nameof(RenderChartTool.RenderChart))!
                .GetParameters().Single(p => p.Name == "chartType")
                .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                .Cast<System.ComponentModel.DescriptionAttribute>().Single().Description;

            foreach (var type in advertised)
            {
                description.Should().Contain(type, "the model is only told about the types the tool description names");
            }
        }

        private static CachedTable BuildWideTable()
        {
            var rows = Enumerable.Range(0, 6).Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["Day"] = "D" + i,
                ["Alpha"] = 10.0 + i,
                ["Beta"] = 20.0 + i * 2,
                ["Open"] = 10.0 + i,
                ["High"] = 14.0 + i,
                ["Low"] = 8.0 + i,
                ["Close"] = 12.0 + i,
                ["Min"] = 1.0 + i,
                ["Q1"] = 2.0 + i,
                ["Med"] = 3.0 + i,
                ["Q3"] = 4.0 + i,
                ["Max"] = 5.0 + i,
                ["Row"] = "R" + i / 3,
                ["Col"] = "C" + i % 3,
                ["Angle"] = i * 40.0,
                ["Lat"] = 10.0 + i * 8,
                ["Lon"] = -60.0 + i * 25,
                ["Country"] = new[] { "FRA", "DEU", "BRA", "USA", "IND", "CHN" }[i],
                ["Start"] = new DateTime(2026, 1, 1 + i * 3, 0, 0, 0, DateTimeKind.Utc),
                ["End"] = new DateTime(2026, 1, 3 + i * 3, 0, 0, 0, DateTimeKind.Utc)
            }).ToList();

            return new CachedTable(rows[0].Keys.Select(k => new CachedColumn(k, "x")).ToList(), rows);
        }

        public static IEnumerable<object?[]> KindsWithTheirArguments()
        {
            return
            [
                ["stackedcolumn", "Day", "Alpha, Beta", null],
                ["stackedbar", "Day", "Alpha, Beta", null],
                ["percentstackedcolumn", "Day", "Alpha, Beta", null],
                ["percentstackedbar", "Day", "Alpha, Beta", null],
                ["stackedarea", "Day", "Alpha, Beta", null],
                ["percentstackedarea", "Day", "Alpha, Beta", null],
                ["combo", "Day", "Alpha, Beta", null],
                ["radar", "Day", "Alpha, Beta", null],
                ["waterfall", "Day", "Alpha", null],
                ["funnel", "Day", "Alpha", null],
                ["treemap", "Day", "Alpha", null],
                ["bullet", "Day", "Alpha, Beta", null],
                ["sparkline", string.Empty, "Alpha", null],
                ["heatmap", string.Empty, string.Empty, "Row=Row;Column=Col;Value=Alpha"],
                ["boxplot", string.Empty, string.Empty,
                    "Category=Day;Minimum=Min;FirstQuartile=Q1;Median=Med;ThirdQuartile=Q3;Maximum=Max"],
                ["candlestick", string.Empty, string.Empty, "Category=Day;Open=Open;High=High;Low=Low;Close=Close"],
                ["polar", string.Empty, string.Empty, "X=Angle;Y=Alpha"],
                ["gauge", string.Empty, string.Empty, "Value=Alpha;Target=Beta"],
                ["gantt", string.Empty, string.Empty, "Category=Day;Start=Start;End=End"],
                ["map", string.Empty, string.Empty, "Latitude=Lat;Longitude=Lon;Value=Beta;Label=Day"]
            ];
        }

        [Theory]
        [MemberData(nameof(KindsWithTheirArguments))]
        public async Task EveryKindBeyondTheOriginalEightIsDrawnFromACachedTableAsync(string chartType, string category,
            string values, string? roles)
        {
            var export = new FakeExport();

            var result = await BuildTool(export, BuildWideTable()).RenderChart("ptr", chartType, category, values,
                null, null, roles);

            result.Should().StartWith("Drew a " + chartType);
            export.Files.Should().ContainSingle();

            var svg = Encoding.UTF8.GetString(export.Bytes.Single());
            svg.Should().StartWith("<svg").And.Contain("<style").And.NotContain("<script");
            svg.Should().NotContain("tabindex", "an exported picture is static, not interactive");
        }

        [Fact]
        public async Task ACountryMapIsDrawnFromCountryAndValueRolesAsync()
        {
            var export = new FakeExport();

            var result = await BuildTool(export, BuildWideTable()).RenderChart("ptr", "map", string.Empty,
                string.Empty, null, null, "Country=Country;Value=Alpha");

            result.Should().StartWith("Drew a map");
            Encoding.UTF8.GetString(export.Bytes.Single()).Should().Contain("France");
        }

        [Theory]
        [InlineData("Box Plot")]
        [InlineData("BOXPLOT")]
        [InlineData("box-plot")]
        public async Task AChartTypeIsMatchedWhateverItsSpacingOrCaseAsync(string chartType)
        {
            var export = new FakeExport();

            var result = await BuildTool(export, BuildWideTable()).RenderChart("ptr", chartType, string.Empty,
                string.Empty, null, null,
                "Category=Day;Minimum=Min;FirstQuartile=Q1;Median=Med;ThirdQuartile=Q3;Maximum=Max");

            result.Should().StartWith("Drew a");
            export.Files.Should().ContainSingle();
        }

        [Fact]
        public async Task WithNoRolesTheCategoryAndValueColumnsAreUsedInOrderAndTheRestIgnoredAsync()
        {
            var export = new FakeExport();

            await BuildTool(export, BuildWideTable()).RenderChart("ptr", "stackedcolumn", "Day", "Beta");

            var svg = Encoding.UTF8.GetString(export.Bytes.Single());
            svg.Should().Contain("Beta").And.NotContain("Alpha");
        }

        [Theory]
        [InlineData("Nonsense=Alpha", "not a chart role")]
        [InlineData("Value=Missing", "Missing")]
        [InlineData("Value", "not a Role=Column pair")]
        [InlineData("Value=", "not a Role=Column pair")]
        public async Task ABadRolesArgumentIsRefusedWithAReasonTheModelCanActOnAsync(string roles, string expected)
        {
            var export = new FakeExport();

            var result = await BuildTool(export, BuildWideTable()).RenderChart("ptr", "funnel", "Day", "Alpha",
                null, null, roles);

            result.Should().StartWith("Could not draw that chart").And.Contain(expected);
            export.Files.Should().BeEmpty();
        }

        [Fact]
        public async Task AKindThatCannotBeSatisfiedNamesTheMissingRoleAsync()
        {
            var export = new FakeExport();

            var result = await BuildTool(export, BuildWideTable()).RenderChart("ptr", "candlestick", "Day", "Alpha");

            result.Should().StartWith("Could not draw that chart").And.Contain("High").And.NotContain("no a ");
            export.Files.Should().BeEmpty();
        }

        [Fact]
        public async Task ATypeThatDoesNotExistListsTheRealOnesIncludingTheNewKindsAsync()
        {
            var result = await BuildTool(new FakeExport()).RenderChart("ptr", "sunburst", "RuleName", "Hits");

            result.Should().Contain("boxplot").And.Contain("column");
        }

        [Fact]
        public async Task AHistogramNeedsNoCategoryColumnBecauseItLabelsItsOwnAxisAsync()
        {
            var export = new FakeExport();

            var result = await BuildTool(export).RenderChart(
                "ptr", "histogram", string.Empty, "Hits", "Amounts");

            result.Should().StartWith("Drew a histogram of 3 value(s) of Hits",
                "a histogram plots the values, not the rows, so describing it by category would be wrong");

            export.Files.Should().ContainSingle();
        }

        [Fact]
        public void AHistogramBinsTheRawValuesRatherThanDrawingOneMarkPerRow()
        {
            var many = new CachedTable(
                [new CachedColumn("Amount", "double")],
                [
                    .. Enumerable.Range(1, 500)
                        .Select(i => new Dictionary<string, object?> { ["Amount"] = (double)(i % 50) })
                ]);

            var svg = Encoding.UTF8.GetString(new AskJoobyChartRenderer().Render(many,
                new ChartSpecification("histogram", string.Empty, ["Amount"], "Amounts")));

            svg.Should().StartWith("<svg",
                "five hundred values is a better histogram than fifty, and the row ceiling that keeps a "
                + "categorical axis readable has no bearing on one");
        }

        [Fact]
        public void ABinCountIsHonouredBecauseAThresholdHasToLandOnAnEdge()
        {
            var table = new CachedTable(
                [new CachedColumn("Amount", "double")],
                [
                    .. Enumerable.Range(1, 100)
                        .Select(i => new Dictionary<string, object?> { ["Amount"] = (double)(i * 100) })
                ]);

            var renderer = new AskJoobyChartRenderer();

            var chosen = Encoding.UTF8.GetString(renderer.Render(table,
                new ChartSpecification("histogram", string.Empty, ["Amount"], "A", Bins: 5)));

            var automatic = Encoding.UTF8.GetString(renderer.Render(table,
                new ChartSpecification("histogram", string.Empty, ["Amount"], "A")));

            chosen.Should().NotBe(automatic,
                "a default binning straddles a reporting threshold and hides the very thing being looked "
                + "for, so asking for a bin count has to actually change the bins");
        }

        [Fact]
        public void AnOrdinaryChartStillDemandsItsCategoryColumn()
        {
            var render = () => new AskJoobyChartRenderer().Render(BuildTable(),
                new ChartSpecification("column", string.Empty, ["Hits"], "A"));

            render.Should().Throw<ArgumentException>().WithMessage("*categoryColumn*");
        }

        [Fact]
        public void TheRendererDrawsWithInlineStylesSoAnImageTagCanShowIt()
        {
            var svg = Encoding.UTF8.GetString(new AskJoobyChartRenderer().Render(BuildTable(),
                new ChartSpecification("column", "RuleName", ["Hits"], "Hits by rule")));

            svg.Should().Contain("<style",
                "an SVG shown through an img tag cannot reach the page's stylesheet, so a chart "
                + "rendered for external styles arrives unstyled");
        }

        private sealed class FakeCache(CachedTable? table) : IToolResultCache
        {
            public Task<string> StoreAsync(CachedTable stored, string? label = null, TimeSpan? ttl = null,
                CancellationToken token = default)
            {
                return Task.FromResult("tbl_fake");
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                return Task.FromResult(table);
            }
        }

        private sealed class FakeExport : IFileExportClient, IExportSink
        {
            private readonly List<ExportedFile> files = [];
            public List<byte[]> Bytes { get; } = [];
            public List<string> ContentTypes { get; } = [];
            public IReadOnlyList<ExportedFile> Files => files;

            public void Record(int fileId, string fileName)
            {
                files.Add(new ExportedFile(fileId, fileName));
            }

            public Task<int> ExportAsync(byte[] content, string fileName, string contentType,
                CancellationToken token = default)
            {
                Bytes.Add(content);
                ContentTypes.Add(contentType);

                return Task.FromResult(Bytes.Count);
            }
        }
    }
}
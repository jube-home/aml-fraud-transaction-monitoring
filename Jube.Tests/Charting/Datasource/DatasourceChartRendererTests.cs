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
using FluentAssertions;
using Jube.Charting.Core.Models;
using Jube.Dto.Options;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Models;
using Xunit;

namespace Jube.Test.Charting.Datasource
{
    [Trait("Category", "Unit")]
    public sealed class DatasourceChartRendererTests
    {
        private static readonly DatasourceChartPresentation external = new();

        private static readonly DatasourceChartPresentation inline = new(ChartStyleMode.Inline, ThemeMode.Dark);

        private static readonly (string Name, SeriesDataType Type) Day = ("Day", SeriesDataType.Text);
        private static readonly (string Name, SeriesDataType Type) Alpha = ("Alpha", SeriesDataType.Decimal);
        private static readonly (string Name, SeriesDataType Type) Beta = ("Beta", SeriesDataType.Integer);

        private static DatasourceFrame Frame((string Name, SeriesDataType Type)[] columns, params object?[][] rows)
        {
            return new DatasourceFrame(
                columns.Select((c, i) => new DatasourceColumn(c.Name, i + 1, c.Type.ToString(), c.Type)).ToList(),
                rows);
        }

        private static object?[][] Rows(int count, Func<int, object?[]> row)
        {
            return Enumerable.Range(0, count).Select(row).ToArray();
        }

        private static DatasourceFrame Categorical()
        {
            return Frame([Day, Alpha, Beta], Rows(6, i => [$"D{i}", 1.5 + i, 10 + i * 2]));
        }

        private static Dictionary<DatasourceDisplayKind, DatasourceFrame> Samples()
        {
            var categorical = Categorical();
            var all = new Dictionary<DatasourceDisplayKind, DatasourceFrame>();

            foreach (var kind in new[]
                     {
                         DatasourceDisplayKind.Column, DatasourceDisplayKind.Bar, DatasourceDisplayKind.StackedColumn,
                         DatasourceDisplayKind.StackedBar, DatasourceDisplayKind.PercentStackedColumn,
                         DatasourceDisplayKind.PercentStackedBar, DatasourceDisplayKind.Line,
                         DatasourceDisplayKind.Area, DatasourceDisplayKind.StackedArea,
                         DatasourceDisplayKind.PercentStackedArea, DatasourceDisplayKind.Pie,
                         DatasourceDisplayKind.Donut, DatasourceDisplayKind.Combo, DatasourceDisplayKind.Waterfall,
                         DatasourceDisplayKind.Funnel, DatasourceDisplayKind.Radar, DatasourceDisplayKind.Bullet,
                         DatasourceDisplayKind.Sparkline, DatasourceDisplayKind.Treemap
                     })
            {
                all[kind] = categorical;
            }

            var x = ("X", SeriesDataType.Decimal);
            var y = ("Y", SeriesDataType.Decimal);
            all[DatasourceDisplayKind.Scatter] = Frame([x, y], Rows(12, i => [i * 1.0, i * i * 0.5]));
            all[DatasourceDisplayKind.Polar] = Frame([x, y], Rows(12, i => [i * 30.0, 1.0 + i % 4]));
            all[DatasourceDisplayKind.Histogram] = Frame([Alpha], Rows(40, i => [Math.Sin(i) * 10]));

            all[DatasourceDisplayKind.Heatmap] = Frame(
                [("RowCat", SeriesDataType.Text), ("ColCat", SeriesDataType.Text), ("V", SeriesDataType.Decimal)],
                Rows(9, i => [$"R{i / 3}", $"C{i % 3}", i * 1.0]));

            all[DatasourceDisplayKind.BoxPlot] = Frame(
                [Day, ("Min", SeriesDataType.Decimal), ("Q1", SeriesDataType.Decimal),
                    ("Med", SeriesDataType.Decimal), ("Q3", SeriesDataType.Decimal), ("Max", SeriesDataType.Decimal)],
                Rows(4, i => [$"G{i}", 1.0 + i, 2.0 + i, 3.0 + i, 4.0 + i, 5.0 + i]));

            all[DatasourceDisplayKind.Candlestick] = Frame(
                [Day, ("Open", SeriesDataType.Decimal), ("High", SeriesDataType.Decimal),
                    ("Low", SeriesDataType.Decimal), ("Close", SeriesDataType.Decimal)],
                Rows(6, i => [$"D{i}", 10.0 + i, 14.0 + i, 8.0 + i, 12.0 + i]));

            all[DatasourceDisplayKind.Gauge] = Frame([("Score", SeriesDataType.Decimal), ("Goal", SeriesDataType.Decimal)],
                [[62.0, 80.0]]);

            all[DatasourceDisplayKind.Map] = Frame(
                [("Latitude", SeriesDataType.Decimal), ("Longitude", SeriesDataType.Decimal),
                    ("Visits", SeriesDataType.Integer)],
                Rows(5, i => [10.0 + i * 8, -60.0 + i * 25, 5 + i * 3]));

            all[DatasourceDisplayKind.Gantt] = Frame(
                [("Task", SeriesDataType.Text), ("Start", SeriesDataType.DateTime), ("End", SeriesDataType.DateTime)],
                Rows(3, i => [$"T{i}", new DateTime(2026, 1, 1 + i * 3, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 1, 3 + i * 3, 0, 0, 0, DateTimeKind.Utc)]));

            return all;
        }

        private static DatasourceDisplayKind[] DrawableKinds()
        {
            return Enum.GetValues<DatasourceDisplayKind>().Where(k => k != DatasourceDisplayKind.Template).ToArray();
        }

        public static IEnumerable<object[]> Kinds()
        {
            return DrawableKinds().Select(k => new object[] { k });
        }

        [Fact]
        public void EveryDrawableKindIsRegisteredOnceWithAUniqueNameAndANonEmptyRoleList()
        {
            var registered = DatasourceChartKinds.All;

            registered.Select(k => k.Kind).Should().OnlyHaveUniqueItems();
            registered.Select(k => k.Name).Should().OnlyHaveUniqueItems();
            DrawableKinds().Should().BeSubsetOf(registered.Select(k => k.Kind));
            registered.Should().OnlyContain(k => (k.Roles.Count > 0 || k.Kind == DatasourceDisplayKind.Template) && !string.IsNullOrWhiteSpace(k.Description));
            registered.Should().OnlyContain(k => k.Roles.Select(r => r.Role).Distinct().Count() == k.Roles.Count);
        }

        [Fact]
        public void EveryDrawableKindHasASampleFrame()
        {
            Samples().Keys.Should().BeEquivalentTo(DrawableKinds());
        }

        [Theory]
        [MemberData(nameof(Kinds))]
        public void EveryKindDrawsItsSampleByPositionAlone(DatasourceDisplayKind kind)
        {
            var svg = DatasourceChartRenderer.Render(kind, Samples()[kind], null, external);

            SvgAssert.WellFormed(svg);
            svg.Should().StartWith("<svg").And.NotContain("<script").And.NotContain("NaN");
            svg.Should().NotContain("<style");
        }

        [Theory]
        [MemberData(nameof(Kinds))]
        public void EveryKindDrawsASelfContainedDarkSvgInInlineMode(DatasourceDisplayKind kind)
        {
            var svg = DatasourceChartRenderer.Render(kind, Samples()[kind], null, inline);

            SvgAssert.WellFormed(svg);
            svg.Should().Contain("<style").And.Contain("data-theme=\"dark\"");
        }

        [Fact]
        public void ColumnsAfterTheCategoryBecomeOneSeriesEach()
        {
            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Column, Categorical(), null, external);

            svg.Should().Contain("Alpha").And.Contain("Beta");
        }

        [Fact]
        public void AnExplicitMappingSelectsTheSeriesAndLeavesTheOtherColumnsForTheGrid()
        {
            var frame = Frame([Day, Alpha, ("Notes", SeriesDataType.Text), Beta],
                Rows(4, i => [$"D{i}", 1.0 + i, "note", 5 + i]));

            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Column, frame,
                new Dictionary<DatasourceChartRole, IReadOnlyList<string>>
                {
                    [DatasourceChartRole.Value] = ["Beta"]
                }, external);

            svg.Should().Contain("Beta").And.NotContain("Alpha");
        }

        [Fact]
        public void ThePositionalDefaultPutsTheFirstNumericColumnOnColumnsAndTheRestOnLinesInACombo()
        {
            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Combo);

            var mapping = DatasourceChartMappingResolver.Resolve(kind, Categorical(), null);

            mapping.Many(DatasourceChartRole.Value).Should().Equal("Alpha");
            mapping.Many(DatasourceChartRole.Line).Should().Equal("Beta");
            mapping.Has(DatasourceChartRole.SecondaryLine).Should().BeFalse();
        }

        [Fact]
        public void OptionalRolesAreNotTakenByPositionUnlessTheyAreMarkedAutomatic()
        {
            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Column);
            var frame = Frame([Day, Alpha, ("Region", SeriesDataType.Text)], Rows(2, i => [$"D{i}", 1.0 + i, "north"]));

            var mapping = DatasourceChartMappingResolver.Resolve(kind, frame, null);

            mapping.Has(DatasourceChartRole.Series).Should().BeFalse();
        }

        [Fact]
        public void ABulletTakesItsTargetFromTheNextNumericColumnWhenThereIsOne()
        {
            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Bullet);

            var mapping = DatasourceChartMappingResolver.Resolve(kind, Categorical(), null);

            mapping.One(DatasourceChartRole.Category).Should().Be("Day");
            mapping.One(DatasourceChartRole.Value).Should().Be("Alpha");
            mapping.One(DatasourceChartRole.Target).Should().Be("Beta");
        }

        [Fact]
        public void ALongFormResultIsPivotedIntoOneSeriesPerDistinctSeriesValue()
        {
            var frame = Frame([Day, ("Region", SeriesDataType.Text), Alpha],
                [["Mon", "north", 1.0], ["Mon", "south", 2.0], ["Tue", "north", 3.0], ["Tue", "south", 4.0]]);

            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Column, frame,
                new Dictionary<DatasourceChartRole, IReadOnlyList<string>>
                {
                    [DatasourceChartRole.Series] = ["Region"]
                }, external);

            svg.Should().Contain("north").And.Contain("south");
        }

        [Fact]
        public void APivotRefusesTwoRowsForTheSameCategoryAndSeries()
        {
            var frame = Frame([Day, ("Region", SeriesDataType.Text), Alpha],
                [["Mon", "north", 1.0], ["Mon", "north", 2.0]]);

            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Column, frame,
                new Dictionary<DatasourceChartRole, IReadOnlyList<string>>
                {
                    [DatasourceChartRole.Series] = ["Region"]
                }, external);

            act.Should().Throw<DatasourceChartException>().WithMessage("*more than one row*Aggregate it in SQL*");
        }

        [Fact]
        public void ALineOverATimeColumnDrawsATimeAxis()
        {
            var frame = Frame([("At", SeriesDataType.DateTime), Alpha],
                Rows(5, i => [new DateTime(2026, 1, 1 + i, 0, 0, 0, DateTimeKind.Utc), 1.0 + i]));

            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Line, frame, null, external);

            SvgAssert.WellFormed(svg);
            svg.Should().Contain("Alpha");
        }

        [Fact]
        public void ANumericCategoryColumnDrawsALineAgainstTheNumbers()
        {
            var frame = Frame([("Step", SeriesDataType.Integer), Alpha], Rows(5, i => [i * 10, 1.0 + i]));

            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Line, frame, null, external);

            SvgAssert.WellFormed(svg);
        }

        [Theory]
        [InlineData("Missing")]
        [InlineData("Day")]
        public void AnOverrideNamingAMissingOrNonNumericColumnIsRefusedWithAReason(string column)
        {
            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Column, Categorical(),
                new Dictionary<DatasourceChartRole, IReadOnlyList<string>>
                {
                    [DatasourceChartRole.Value] = [column]
                }, external);

            act.Should().Throw<DatasourceChartException>().WithMessage($"*\"{column}\"*");
        }

        [Fact]
        public void AnOverrideForARoleTheChartDoesNotUseIsRefused()
        {
            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Pie, Categorical(),
                new Dictionary<DatasourceChartRole, IReadOnlyList<string>>
                {
                    [DatasourceChartRole.Latitude] = ["Alpha"]
                }, external);

            act.Should().Throw<DatasourceChartException>().WithMessage("*does not use the role Latitude*");
        }

        [Fact]
        public void AResultWithNoSuitableColumnForARoleSaysWhichRoleIsUnsatisfied()
        {
            var frame = Frame([Day], Rows(3, i => [$"D{i}"]));

            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Column, frame, null, external);

            act.Should().Throw<DatasourceChartException>().WithMessage("*Value*numeric column*");
        }

        [Fact]
        public void SplittingBySeriesWithSeveralValueColumnsIsRefused()
        {
            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Column,
                Frame([Day, Alpha, Beta, ("Region", SeriesDataType.Text)], Rows(2, i => [$"D{i}", 1.0, 2, "n"])),
                new Dictionary<DatasourceChartRole, IReadOnlyList<string>>
                {
                    [DatasourceChartRole.Series] = ["Region"]
                }, external);

            act.Should().Throw<DatasourceChartException>().WithMessage("*exactly one value column*");
        }

        [Fact]
        public void NoRowsIsExplainedNotThrownAsAnArgumentFault()
        {
            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Column,
                Frame([Day, Alpha], []), null, external);

            act.Should().Throw<DatasourceChartException>().WithMessage("*no rows*");
        }

        [Fact]
        public void TooManyCategoriesIsExplainedWithAHintToAggregate()
        {
            var frame = Frame([Day, Alpha], Rows(2100, i => [$"D{i}", 1.0]));

            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Column, frame, null, external);

            act.Should().Throw<DatasourceChartException>().WithMessage("*Aggregate the query in SQL*");
        }

        [Fact]
        public void NullAndNonFiniteValuesBecomeGapsAndNeverReachTheSvg()
        {
            var frame = Frame([Day, Alpha], [["a", 1.0], ["b", null], ["c", double.NaN], ["d", 4.0]]);

            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Line, frame, null, external);

            svg.Should().NotContain("NaN").And.NotContain("Infinity");
        }

        [Fact]
        public void TextInTheDataCannotInjectMarkup()
        {
            var frame = Frame([Day, Alpha], [["<script>alert(1)</script>", 1.0], ["\"onload=\"x", 2.0]]);

            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Column, frame, null, external);

            SvgAssert.WellFormed(svg);
            svg.Should().NotContain("<script");
        }


        [Fact]
        public void APointMapFindsItsLatitudeAndLongitudeByColumnNameAndSizesByTheNextNumber()
        {
            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Map);

            var mapping = DatasourceChartMappingResolver.Resolve(kind, Samples()[DatasourceDisplayKind.Map], null);

            mapping.One(DatasourceChartRole.Latitude).Should().Be("Latitude");
            mapping.One(DatasourceChartRole.Longitude).Should().Be("Longitude");
            mapping.One(DatasourceChartRole.Value).Should().Be("Visits");
            mapping.Has(DatasourceChartRole.Country).Should().BeFalse();
        }

        [Fact]
        public void ACountryMapFindsItsCountryByNameAndColoursByTheNumber()
        {
            var frame = Frame([("CountryCode", SeriesDataType.Text), ("Score", SeriesDataType.Decimal)],
                [["FRA", 1.0], ["DEU", 5.0], ["BRA", 9.0]]);

            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Map, frame, null, external);

            svg.Should().Contain("France").And.Contain("Score");
        }

        [Fact]
        public void ALoneNumericColumnIsNeverMistakenForALatitude()
        {
            var frame = Frame([("Where", SeriesDataType.Text), ("Score", SeriesDataType.Decimal)], [["FRA", 1.0]]);

            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Map, frame, null, external);

            act.Should().Throw<DatasourceChartException>().WithMessage("*latitude and a longitude column, or a country column*");
        }

        [Fact]
        public void AnExplicitMappingMakesAMapOutOfColumnsWithNoObviousNames()
        {
            var frame = Frame([("A", SeriesDataType.Decimal), ("B", SeriesDataType.Decimal)], [[48.9, 2.4]]);

            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Map, frame,
                new Dictionary<DatasourceChartRole, IReadOnlyList<string>>
                {
                    [DatasourceChartRole.Latitude] = ["A"],
                    [DatasourceChartRole.Longitude] = ["B"]
                }, external);

            SvgAssert.WellFormed(svg);
        }

        [Fact]
        public void ExplicitLatitudeAndLongitudeStopACountryNamedColumnTurningAPointMapIntoACountryMap()
        {
            var frame = Frame(
                [("Country", SeriesDataType.Text), ("A", SeriesDataType.Decimal), ("B", SeriesDataType.Decimal)],
                [["FRA", 48.9, 2.4]]);

            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Map);

            var mapping = DatasourceChartMappingResolver.Resolve(kind, frame,
                new Dictionary<DatasourceChartRole, IReadOnlyList<string>>
                {
                    [DatasourceChartRole.Latitude] = ["A"],
                    [DatasourceChartRole.Longitude] = ["B"]
                });

            mapping.Has(DatasourceChartRole.Country).Should().BeFalse();
        }

        [Fact]
        public void AnExplicitCountryStopsLatitudeAndLongitudeNamedColumnsTurningACountryMapIntoAPointMap()
        {
            var frame = Frame(
                [("Where", SeriesDataType.Text), ("Latitude", SeriesDataType.Decimal),
                    ("Longitude", SeriesDataType.Decimal), ("Score", SeriesDataType.Decimal)],
                [["FRA", 48.9, 2.4, 3.0]]);

            var kind = DatasourceChartKinds.Get(DatasourceDisplayKind.Map);

            var mapping = DatasourceChartMappingResolver.Resolve(kind, frame,
                new Dictionary<DatasourceChartRole, IReadOnlyList<string>>
                {
                    [DatasourceChartRole.Country] = ["Where"]
                });

            mapping.Has(DatasourceChartRole.Latitude).Should().BeFalse();
            mapping.One(DatasourceChartRole.Value).Should().Be("Latitude");
        }

        [Fact]
        public void AnUnsatisfiedRoleIsExplainedInPlainGrammar()
        {
            var frame = Frame([Day, Alpha], Rows(2, i => [$"D{i}", 1.0 + i]));

            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Candlestick, frame, null, external);

            act.Should().Throw<DatasourceChartException>().WithMessage("*no unused numeric column is left*")
                .Where(e => !e.Message.Contains("no a "));
        }

        [Fact]
        public void ALatitudeWithoutALongitudeIsRefused()
        {
            var frame = Frame([("Latitude", SeriesDataType.Decimal), ("Score", SeriesDataType.Decimal)], [[48.9, 2.4]]);

            var act = () => DatasourceChartRenderer.Render(DatasourceDisplayKind.Map, frame, null, external);

            act.Should().Throw<DatasourceChartException>().WithMessage("*both a latitude and a longitude*");
        }

        [Fact]
        public void AMapIsNoTallerThanTheWorldIsWideSoItLeavesNoEmptyBands()
        {
            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Map, Samples()[DatasourceDisplayKind.Map],
                null, new DatasourceChartPresentation(Width: 640, Height: 400));

            svg.Should().Contain("0 0 640 312");
        }

        [Fact]
        public void AMapKeepsAShorterRequestedHeight()
        {
            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Map, Samples()[DatasourceDisplayKind.Map],
                null, new DatasourceChartPresentation(Width: 640, Height: 200));

            svg.Should().Contain("0 0 640 200");
        }

        [Fact]
        public void ThePresentationTitleAndSizeReachTheSvg()
        {
            var svg = DatasourceChartRenderer.Render(DatasourceDisplayKind.Column, Categorical(), null,
                new DatasourceChartPresentation(Title: "Revenue by day", Width: 900, Height: 300));

            svg.Should().Contain("Revenue by day").And.Contain("0 0 900 300");
        }
    }
}

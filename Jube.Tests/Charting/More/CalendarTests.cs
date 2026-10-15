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
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Exceptions;
using Jube.Charting.More;
using Jube.Charting.More.Models;
using Xunit;

namespace Jube.Test.Charting.More
{
    public class CalendarTests
    {
        [Fact]
        public void EveryDayOfEveryYearGetsACellAndDataDaysGetMarks()
        {
            var svg = Chart.CalendarHeatmap().Day(new DateTime(2024, 3, 1), 5).Day(new DateTime(2025, 6, 2), 9)
                .Render();

            Regex.Matches(svg, "class=\"cell (m )?(q\\d|na)[ \"]").Count.Should().Be(366 + 365 + 9);
            Regex.Matches(svg, "class=\"mk\"").Count.Should().Be(2);

            svg.Should().Contain("Fri 1 Mar 2024").And.Contain("Mon 2 Jun 2025").And.Contain("2024").And
                .Contain("2025");

            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void CellsAreLaidOutByWeekColumnAndWeekdayRow()
        {
            var svg = Chart.CalendarHeatmap().Day(new DateTime(2025, 1, 1), 1).Render();
            svg.Should().Contain(">Mon<").And.Contain(">Wed<").And.Contain(">Fri<");
            var sunday = Chart.CalendarHeatmap().WeekStartsOnSunday().Day(new DateTime(2025, 1, 1), 1).Render();
            sunday.Should().Contain(">Sun<").And.Contain(">Tue<").And.Contain(">Thu<");
            var monday = Regex.Matches(svg, "<rect class=\"cell m q\\d\" x=\"([0-9.]+)\" y=\"([0-9.]+)\"").Single();
            var sundayCell = Regex.Matches(sunday, "<rect class=\"cell m q\\d\" x=\"([0-9.]+)\" y=\"([0-9.]+)\"")
                .Single();

            double.Parse(sundayCell.Groups[2].Value).Should().BeGreaterThan(double.Parse(monday.Groups[2].Value));
        }

        [Fact]
        public void DuplicateDatesAreAggregatedAndMissingOrNonFiniteValuesAreSkipped()
        {
            var day = new DateTime(2025, 5, 5);
            var sum = Chart.CalendarHeatmap().Day(day, 2).Day(day, 3).Day(day.AddDays(1), double.NaN).Render();
            sum.Should().Contain("Value: 5");
            Regex.Matches(sum, "class=\"mk\"").Count.Should().Be(1);

            Chart.CalendarHeatmap().Aggregate(CalendarAggregate.Max).Day(day, 2).Day(day, 3).Render().Should()
                .Contain("Value: 3");
            Chart.CalendarHeatmap().Aggregate(CalendarAggregate.Mean).Day(day, 2).Day(day, 4).Render().Should()
                .Contain("Value: 3");
        }

        [Fact]
        public void DomainAndLegendReflectTheRange()
        {
            var svg = Chart.CalendarHeatmap().Range(0, 100).Day(new DateTime(2025, 1, 1), 50).Render();
            svg.Should().Contain("class=\"cell m q4\"").And.Contain(">100<");

            var negative = Chart.CalendarHeatmap().Day(new DateTime(2025, 1, 1), -5).Day(new DateTime(2025, 1, 2), 5)
                .Render();
            negative.Should().Contain(">-5<");
        }

        [Fact]
        public void CapsSpecsAndEmptyAreHandled()
        {
            var builder = Chart.CalendarHeatmap();

            for (var y = 2000; y <= 2000 + CalendarChart.MaxYears; y++)
            {
                builder.Day(new DateTime(y, 1, 1), 1);
            }

            Action years = () => builder.Render();
            years.Should().Throw<ChartLimitException>();
            Action length = () => Chart.CalendarHeatmap().Days([DateTime.Today], [1.0, 2]);
            length.Should().Throw<ChartSpecificationException>();
            var empty = Chart.CalendarHeatmap().Render();
            empty.Should().Contain("No data");
            MoreAssert.IsSafe(empty);

            var many = Enumerable.Range(0, ChartLimits.MaxPoints + 1)
                .Select(i => new DateTime(2025, 1, 1).AddDays(i % 300)).ToArray();

            Action points = () => Chart.CalendarHeatmap().Days(many, many.Select(_ => 1.0)).Render();
            points.Should().Throw<ChartLimitException>();
        }

        [Fact]
        public void HeightGrowsWithTheNumberOfYears()
        {
            double Height(string svg)
            {
                return double.Parse(Regex.Match(svg, "viewBox=\"0 0 [0-9.]+ ([0-9.]+)\"").Groups[1].Value);
            }

            var one = Chart.CalendarHeatmap().Day(new DateTime(2025, 1, 1), 1).Render();

            var three = Chart.CalendarHeatmap().Day(new DateTime(2023, 1, 1), 1).Day(new DateTime(2025, 1, 1), 1)
                .Render();

            Height(three).Should().BeGreaterThan(Height(one));
        }
    }
}
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
using FluentAssertions;
using Jube.Analytics.Categorical;
using Jube.Analytics.Datasets;
using Jube.Analytics.Datasets.Models;
using Xunit;

namespace Jube.Test.Exhaustive.Datasets
{
    public class DatasetCategoricalSupportTests
    {
        private static (DatasetColumnDefinition Column, DatasetColumnAccumulator Accumulator) BuildTextColumn(
            int index, string header, IEnumerable<string> values)
        {
            var accumulator = new DatasetColumnAccumulator();

            foreach (var value in values)
            {
                accumulator.AddText(value, DatasetLimits.Default);
            }

            return (new DatasetColumnDefinition { Index = index, Header = header }, accumulator);
        }

        private static DatasetParseReport Finish(params (DatasetColumnDefinition Column, DatasetColumnAccumulator
            Accumulator)[] columns)
        {
            var report = new DatasetParseReport();

            DatasetProfile.Finish([.. columns.Select(s => s.Column)], [.. columns.Select(s => s.Accumulator)],
                report);

            return report;
        }

        [Fact]
        public void ColumnWithTwoWellPopulatedLevels_IsFittedAsCategoricalButExcludedByDefault()
        {
            var values = Enumerable.Repeat("good", 25).Concat(Enumerable.Repeat("bad", 25));
            var (column, accumulator) = BuildTextColumn(0, "Risk", values);

            Finish((column, accumulator));

            column.Kind.Should().Be(DatasetColumnKinds.Text);
            column.IsCategorical.Should().BeTrue();

            column.Include.Should().BeFalse("a two-level column is often the outcome a Class Definition filter " +
                                            "targets, so it must not be trained on unless the user opts in");

            column.CategoricalEncoderJson.Should().NotBeNullOrWhiteSpace();

            var encoder = CategoricalFieldEncoder.FromJson(column.CategoricalEncoderJson!);
            encoder.Levels.Count.Should().Be(2);
            encoder.Indicators.Count.Should().Be(1);
        }

        [Fact]
        public void IdentifierLikeColumn_IsExcludedWithAReason()
        {
            var values = Enumerable.Range(0, 30).Select(i => "Row-" + i);
            var (column, accumulator) = BuildTextColumn(0, "RowId", values);

            var report = Finish((column, accumulator));

            column.IsCategorical.Should().BeFalse();
            column.Include.Should().BeFalse();
            report.Warnings.Should().Contain(w => w.Contains("RowId") && w.Contains("identifier"));
        }

        [Fact]
        public void DistinctValues_AreCountedAndSortedByFrequency()
        {
            var values = Enumerable.Repeat("SE", 3).Concat(Enumerable.Repeat("NO", 1));
            var (column, accumulator) = BuildTextColumn(0, "Country", values);

            Finish((column, accumulator));

            column.DistinctValues.Should().HaveCount(2);
            column.DistinctValues[0].Value.Should().Be("SE");
            column.DistinctValues[0].Count.Should().Be(3);
            column.DistinctValues[1].Value.Should().Be("NO");
            column.DistinctValues[1].Count.Should().Be(1);
        }

        [Fact]
        public void RawDistinctTracking_TruncatesBeyondTheBudget()
        {
            var accumulator = new DatasetColumnAccumulator();

            for (var i = 0; i < DatasetColumnAccumulator.MaxRawDistinctTracked + 10; i++)
            {
                accumulator.AddText("v" + i, DatasetLimits.Default);
            }

            accumulator.RawDistinct.Count.Should().Be(DatasetColumnAccumulator.MaxRawDistinctTracked);
            accumulator.RawDistinctTruncated.Should().BeTrue();
        }
    }
}
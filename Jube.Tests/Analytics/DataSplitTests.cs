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
using Jube.Analytics;
using Xunit;

namespace Jube.Test.Analytics
{
    public class DataSplitTests
    {
        private static double[] Labels(int n, int positives)
        {
            return [.. Enumerable.Range(0, n).Select(i => i < positives ? 1d : 0d)];
        }

        [Theory]
        [InlineData(1000, 0.6, 0.2, 0.2, true)]
        [InlineData(1000, 0.6, 0.2, 0.2, false)]
        [InlineData(101, 0.5, 0.25, 0.25, true)]
        [InlineData(10, 0.6, 0.2, 0.2, true)]
        public void Split_SetsAreDisjointAndInRange(int n, double train, double validation, double test,
            bool stratify)
        {
            var split = DataSplit.Split(Labels(n, n / 3), train, validation, test, 7, stratify);

            var all = split.Train.Concat(split.Validation).Concat(split.Test).ToArray();
            all.Should().OnlyHaveUniqueItems();
            all.Should().OnlyContain(i => i >= 0 && i < n);
            split.Test.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Split_KnownAnswerSizes_OnExactFractions()
        {
            var split = DataSplit.Split(Labels(1000, 500), 0.6, 0.2, 0.2, 1);
            split.Train.Length.Should().Be(600);
            split.Validation.Length.Should().Be(200);
            split.Test.Length.Should().Be(200);
        }

        [Fact]
        public void Split_SameSeedIsReproducible_DifferentSeedDiffers()
        {
            var labels = Labels(500, 100);
            var a = DataSplit.Split(labels, 0.6, 0.2, 0.2, 42);
            var b = DataSplit.Split(labels, 0.6, 0.2, 0.2, 42);
            var c = DataSplit.Split(labels, 0.6, 0.2, 0.2, 43);

            a.Train.Should().Equal(b.Train);
            a.Validation.Should().Equal(b.Validation);
            a.Test.Should().Equal(b.Test);
            a.Train.Should().NotEqual(c.Train);
        }

        [Fact]
        public void Split_Stratified_PreservesClassProportionInEverySet()
        {
            var labels = Labels(1000, 100);
            var split = DataSplit.Split(labels, 0.6, 0.2, 0.2, 5);

            split.Train.Count(i => labels[i] == 1).Should().Be(60);
            split.Validation.Count(i => labels[i] == 1).Should().Be(20);
            split.Test.Count(i => labels[i] == 1).Should().Be(20);
        }

        [Fact]
        public void Split_Empty_ReturnsEmptySets()
        {
            var split = DataSplit.Split([], 0.6, 0.2, 0.2, 1);
            split.Train.Should().BeEmpty();
            split.Validation.Should().BeEmpty();
            split.Test.Should().BeEmpty();
        }

        [Fact]
        public void Split_SingleRow_NeverDuplicatesTheRow()
        {
            var split = DataSplit.Split([1d], 0.6, 0.2, 0.2, 1);
            (split.Train.Length + split.Validation.Length + split.Test.Length).Should().BeLessThanOrEqualTo(1);
        }

        [Theory]
        [InlineData(0.7, 0.2, 0.2)]
        [InlineData(-0.1, 0.2, 0.2)]
        [InlineData(double.NaN, 0.2, 0.2)]
        [InlineData(1.5, 0, 0)]
        public void Split_InvalidFractions_Throw(double train, double validation, double test)
        {
            var act = () => DataSplit.Split(Labels(10, 5), train, validation, test, 1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Split_NullLabels_Throws()
        {
            var act = () => DataSplit.Split(null!, 0.6, 0.2, 0.2, 1);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Take_GathersRowsInIndexOrder_AndRejectsOutOfRange()
        {
            DataSplit.Take(["a", "b", "c"], [2, 0]).Should().Equal("c", "a");
            var act = () => DataSplit.Take(["a"], [3]);
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}
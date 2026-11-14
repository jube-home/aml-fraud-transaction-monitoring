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


using FluentAssertions;
using Jube.Data.Reporting;
using Jube.Data.Repository.Exceptions;
using Xunit;

namespace Jube.Test.Reporting
{
    [Trait("Category", "Unit")]
    public sealed class ReportLimitsTests
    {
        [Theory]
        [InlineData("30", "1048576", "33554432", 30, 1048576, 33554432)]
        [InlineData("", "x", null, 30, 1048576, 33554432)]
        [InlineData("0", "-5", "0", 30, 1048576, 33554432)]
        [InlineData("5", "10", "20", 5, 10, 20)]
        public void SettingsFallBackToTheDefaultsWhenMissingOrNotPositive(string timeout, string cell, string result,
            int expectedTimeout, int expectedCell, int expectedResult)
        {
            var limits = ReportLimits.From(timeout, cell, result);

            limits.Should().Be(new ReportLimits(expectedTimeout, expectedCell, expectedResult));
        }

        [Fact]
        public void ApproximateBytesCountsStringsBytesAndNestedArrays()
        {
            Postgres.ApproximateBytes(null, 100).Should().Be(0);
            Postgres.ApproximateBytes("abc", 100).Should().Be(3);
            Postgres.ApproximateBytes("é", 100).Should().Be(2);
            Postgres.ApproximateBytes(new byte[40], 100).Should().Be(40);
            Postgres.ApproximateBytes(new[] { "ab", "cd" }, 100).Should().Be(4);
            Postgres.ApproximateBytes(42, 100).Should().Be(16);
        }

        [Fact]
        public void ApproximateBytesStopsEarlyOnceTheCeilingIsPassed()
        {
            Postgres.ApproximateBytes(new string('x', 5000), 10).Should().BeGreaterThan(10);
            Postgres.ApproximateBytes(new[] { new string('x', 50), new string('y', 50) }, 10).Should()
                .BeGreaterThan(10);
        }

        [Fact]
        public void ACodedRefusalSurvivesTheErrorDescriberSoItCanBeLocalised()
        {
            var coded = SqlValidationFailedException.Coded(DatasourceMessages.CellTooLarge, "Big", 10);

            var described = PostgresErrorDescriber.ToException(coded);

            described.Code.Should().Be(DatasourceMessages.CellTooLarge);
            described.Arguments.Should().Equal("Big", 10);
        }
    }
}

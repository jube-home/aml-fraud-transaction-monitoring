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
using Jube.Engine.Exhaustive.Utilities;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class LoadSampleSqlTests
    {
        [Fact]
        public void SampleLimitIsTenThousand()
        {
            Extraction.SampleLimit.Should().Be(10000);
        }

        [Fact]
        public void RealArchiveSample_SelectsFromArchiveOrderedByEntryGuidWithModelIdAndLimitAsTheLastTwoParameters()
        {
            Postgres.BuildArchiveSampleSql("x = (@1)", 3, false).Should().Be(
                "select \"Json\" from \"Archive\" where \"EntityAnalysisModelId\" = (@2) and (x = (@1))"
                + " order by \"EntityAnalysisModelInstanceEntryGuid\" limit (@3)");
        }

        [Fact]
        public void MockArchiveSample_SelectsFromMockArchive()
        {
            Postgres.BuildArchiveSampleSql("1 = 1", 2, true).Should().Be(
                "select \"Json\" from \"MockArchive\" where \"EntityAnalysisModelId\" = (@1) and (1 = 1)"
                + " order by \"EntityAnalysisModelInstanceEntryGuid\" limit (@2)");
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(11)]
        public void ParameterNumbersFollowTheTokenCount(int tokenCount)
        {
            var sql = Postgres.BuildArchiveSampleSql("true", tokenCount, false);

            sql.Should().Contain($"= (@{tokenCount - 1})").And.EndWith($"limit (@{tokenCount})");
        }

        [Fact]
        public void NegatedFilterIsWrappedSoTheNegationCoversTheWholeClause()
        {
            var sql = Postgres.BuildArchiveSampleSql("NOT (a = (@1) or b = (@1))", 3, false);

            sql.Should().Contain("and (NOT (a = (@1) or b = (@1))) order by");
        }

        [Fact]
        public void OrderIsAlwaysByEntryGuidAndNeverRandom()
        {
            var sql = Postgres.BuildArchiveSampleSql("true", 2, false);

            sql.Should().Contain("order by \"EntityAnalysisModelInstanceEntryGuid\"").And.NotContain("random()");
        }
    }
}
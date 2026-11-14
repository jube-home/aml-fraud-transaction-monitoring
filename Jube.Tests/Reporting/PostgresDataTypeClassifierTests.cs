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

using FluentAssertions;
using Jube.Data.Reporting;
using Jube.Dto.Options;
using Xunit;

namespace Jube.Test.Reporting
{
    [Trait("Category", "Unit")]
    public sealed class PostgresDataTypeClassifierTests
    {
        [Theory]
        [InlineData("integer", SeriesDataType.Integer)]
        [InlineData("bigint", SeriesDataType.Integer)]
        [InlineData("int4", SeriesDataType.Integer)]
        [InlineData("double precision", SeriesDataType.Decimal)]
        [InlineData("real", SeriesDataType.Decimal)]
        [InlineData("numeric", SeriesDataType.Decimal)]
        [InlineData("decimal", SeriesDataType.Decimal)]
        [InlineData("smallint", SeriesDataType.Flag)]
        [InlineData("boolean", SeriesDataType.Flag)]
        [InlineData("date", SeriesDataType.DateTime)]
        [InlineData("timestamp without time zone", SeriesDataType.DateTime)]
        [InlineData("timestamp with time zone", SeriesDataType.DateTime)]
        [InlineData("text", SeriesDataType.Text)]
        [InlineData("character varying", SeriesDataType.Text)]
        [InlineData("uuid", SeriesDataType.Text)]
        [InlineData("jsonb", SeriesDataType.Text)]
        [InlineData("double precision[]", SeriesDataType.DecimalArray)]
        [InlineData("numeric[]", SeriesDataType.DecimalArray)]
        [InlineData("text[]", SeriesDataType.Array)]
        [InlineData("integer[]", SeriesDataType.Array)]
        public void ClassifiesTheCommonPostgresTypes(string dataTypeName, SeriesDataType expected)
        {
            PostgresDataTypeClassifier.Classify(dataTypeName).Should().Be(expected);
        }

        [Theory]
        [InlineData("numeric(12,2)", SeriesDataType.Decimal)]
        [InlineData("character varying(50)", SeriesDataType.Text)]
        [InlineData("timestamp(3) without time zone", SeriesDataType.DateTime)]
        [InlineData("  Double   Precision  ", SeriesDataType.Decimal)]
        [InlineData("NUMERIC", SeriesDataType.Decimal)]
        public void IgnoresTypeModifiersCaseAndSpacing(string dataTypeName, SeriesDataType expected)
        {
            PostgresDataTypeClassifier.Classify(dataTypeName).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("unknown")]
        public void TreatsAnythingUnreadableAsText(string? dataTypeName)
        {
            PostgresDataTypeClassifier.Classify(dataTypeName!).Should().Be(SeriesDataType.Text);
        }

        [Fact]
        public void KeepsTheStoredNumbersTheOldViewerAndExistingRowsRelyOn()
        {
            ((int)SeriesDataType.Text).Should().Be(1);
            ((int)SeriesDataType.Integer).Should().Be(2);
            ((int)SeriesDataType.Decimal).Should().Be(3);
            ((int)SeriesDataType.DateTime).Should().Be(4);
            ((int)SeriesDataType.Flag).Should().Be(5);
            ((int)SeriesDataType.DecimalArray).Should().Be(6);
            ((int)SeriesDataType.Array).Should().Be(7);
        }
    }
}

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
using FluentAssertions;
using Jube.Data.Reporting;
using Npgsql;
using Xunit;

namespace Jube.Test.Reporting
{
    [Trait("Category", "Unit")]
    public sealed class PostgresErrorDescriberTests
    {
        [Theory]
        [InlineData("column \"NoSuchColumn\" does not exist", "42703")]
        [InlineData("relation \"NoSuchTable\" does not exist", "42P01")]
        [InlineData("syntax error at or near \"(\"", "42601")]
        [InlineData("invalid input syntax for type integer: \"x\"", "22P02")]
        public void ADatabaseErrorIsDescribedGenericallyWithNothingOfTheDatabaseMessage(string messageText,
            string sqlState)
        {
            var postgres = new PostgresException(messageText, "ERROR", "ERROR", sqlState);

            var described = PostgresErrorDescriber.ToException(new InvalidCastException("outer", postgres));

            described.Code.Should().Be(DatasourceMessages.SqlRejectedGeneric);
            described.Arguments.Should().BeEmpty();

            described.Message.Should().Be(DatasourceMessages.Format(DatasourceMessages.SqlRejectedGeneric, []))
                .And.NotContain(messageText).And.NotContain(sqlState);

            described.InnerException.Should().NotBeNull("the real exception stays attached for server-side logging");
        }
    }
}

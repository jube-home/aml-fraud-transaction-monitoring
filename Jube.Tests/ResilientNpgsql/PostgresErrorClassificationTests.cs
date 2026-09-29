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
using System.Net.Sockets;
using FluentAssertions;
using Jube.ResilientNpgsqlConnection;
using Npgsql;
using Xunit;

namespace Jube.Test.ResilientNpgsql
{
    [Trait("Category", "Unit")]
    public sealed class PostgresErrorClassificationTests
    {
        private static PostgresException For(string sqlState)
        {
            return new PostgresException("message", "ERROR", "ERROR", sqlState);
        }

        [Theory]
        [InlineData("25006")]
        [InlineData("08000")]
        [InlineData("08006")]
        [InlineData("53300")]
        [InlineData("53200")]
        [InlineData("57P01")]
        [InlineData("57P02")]
        [InlineData("57P03")]
        [InlineData("40001")]
        [InlineData("40P01")]
        [InlineData("55P03")]
        public void ARecoverableStateIsRetryable(string sqlState)
        {
            PostgresErrorClassification.IsRetryable(For(sqlState)).Should().BeTrue();
        }

        [Theory]
        [InlineData("42501")]
        [InlineData("23505")]
        [InlineData("22001")]
        [InlineData("25001")]
        public void AnUnrecoverableStateIsNotRetryable(string sqlState)
        {
            PostgresErrorClassification.IsRetryable(For(sqlState)).Should().BeFalse();
        }

        [Fact]
        public void APermissionDeniedIsNotRetryableSoThePurgeFailureSurfacesRatherThanLooping()
        {
            PostgresErrorClassification.IsRetryable(For("42501")).Should().BeFalse();
        }

        [Theory]
        [InlineData("53300")]
        [InlineData("53200")]
        [InlineData("55P03")]
        [InlineData("40001")]
        [InlineData("40P01")]
        public void AStateThatSaysNothingAboutConnectionHealthDoesNotClearThePool(string sqlState)
        {
            PostgresErrorClassification.WarrantsPoolClear(For(sqlState)).Should().BeFalse();
        }

        [Theory]
        [InlineData("25006")]
        [InlineData("08006")]
        [InlineData("57P01")]
        public void AStaleOrBrokenConnectionClearsThePool(string sqlState)
        {
            PostgresErrorClassification.WarrantsPoolClear(For(sqlState)).Should().BeTrue();
        }

        [Fact]
        public void ANonPostgresFailureClearsThePool()
        {
            PostgresErrorClassification.WarrantsPoolClear(new SocketException()).Should().BeTrue();
            PostgresErrorClassification.WarrantsPoolClear(new TimeoutException()).Should().BeTrue();
        }

        [Theory]
        [InlineData("25006")]
        [InlineData("08000")]
        [InlineData("08006")]
        [InlineData("57P01")]
        [InlineData("57P02")]
        [InlineData("57P03")]
        public void AStaleOrBrokenConnectionIsRecycled(string sqlState)
        {
            PostgresErrorClassification.WarrantsConnectionRecycle(For(sqlState)).Should().BeTrue();
        }

        [Theory]
        [InlineData("53300")]
        [InlineData("53200")]
        [InlineData("55P03")]
        [InlineData("40001")]
        [InlineData("40P01")]
        public void AHealthyConnectionIsNotRecycled(string sqlState)
        {
            PostgresErrorClassification.WarrantsConnectionRecycle(For(sqlState)).Should().BeFalse();
        }

        [Fact]
        public void AReadOnlyTransactionIsRetriedRecycledAndClearedTogether()
        {
            var exception = For(PostgresErrorClassification.ReadOnlyTransaction);

            PostgresErrorClassification.IsRetryable(exception).Should().BeTrue();
            PostgresErrorClassification.WarrantsConnectionRecycle(exception).Should().BeTrue();
            PostgresErrorClassification.WarrantsPoolClear(exception).Should().BeTrue();
        }

        [Fact]
        public void TooManyClientsIsRetriedWithoutDisturbingTheConnectionOrThePool()
        {
            var exception = For("53300");

            PostgresErrorClassification.IsRetryable(exception).Should().BeTrue();
            PostgresErrorClassification.WarrantsConnectionRecycle(exception).Should().BeFalse();
            PostgresErrorClassification.WarrantsPoolClear(exception).Should().BeFalse();
        }
    }
}
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
using Jube.ResilientNpgsqlConnection;
using Xunit;

namespace Jube.Test.ResilientNpgsql
{
    [Trait("Category", "Unit")]
    [Collection("PgRetryBudget")]
    public sealed class PgRetryBudgetTests : IDisposable
    {
        private readonly int original = PgRetryBudget.Shared;

        public void Dispose()
        {
            PgRetryBudget.ConfigureShared(original);
        }

        [Fact]
        public void TheDefaultMatchesTheDocumentedSetting()
        {
            PgRetryBudget.DefaultMaxRetries.Should().Be(10);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(10, 10)]
        [InlineData(250, 250)]
        public void AConfiguredBudgetIsWhatTheLayerReads(int configured, int expected)
        {
            PgRetryBudget.ConfigureShared(configured);

            PgRetryBudget.Shared.Should().Be(expected);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void ANegativeBudgetIsFlooredAtNoRetriesRatherThanThrowing(int configured)
        {
            PgRetryBudget.ConfigureShared(configured);

            PgRetryBudget.Shared.Should().Be(0);
        }
    }
}

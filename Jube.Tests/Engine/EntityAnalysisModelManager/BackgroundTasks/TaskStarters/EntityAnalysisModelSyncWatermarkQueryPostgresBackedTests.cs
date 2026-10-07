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

using Jube.Data.Query.GetEntityAnalysisModelSyncWatermarkQuery;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Test.Infrastructure;
using LinqToDB;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelSyncWatermarkQueryPostgresBackedTests
    {
        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        private static async Task GuardSchemaAsync(DbContext dbContext)
        {
            try
            {
                _ = await dbContext.GetTable<EntityAnalysisModelOverride>().Take(1).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "This test needs a running, migrated Jube Postgres schema - point " +
                    "JubeTestConnectionString/ConnectionString at one.", ex);
            }
        }

        [Fact]
        public async Task TheWatermarkQueryTranslatesAndRunsAgainstPostgresAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
            await GuardSchemaAsync(dbContext);

            var watermark = await new GetEntityAnalysisModelSyncWatermarkQuery(dbContext)
                .ExecuteAsync(1, CancellationToken.None);

            using var scope = new AssertionScope();
            watermark.Should().NotBeNull();
            watermark.OverrideCount.Should().BeGreaterThanOrEqualTo(0);
            watermark.OverrideMaxId.Should().BeGreaterThanOrEqualTo(0);
            watermark.ActivationRuleOverrideCount.Should().BeGreaterThanOrEqualTo(0);
            watermark.ActivationRuleOverrideMaxId.Should().BeGreaterThanOrEqualTo(0);
            watermark.ExhaustivePromotedMaxId.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task ATenantWithNoRowsAtAllProducesAZeroWatermarkRatherThanFailingAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
            await GuardSchemaAsync(dbContext);

            var watermark = await new GetEntityAnalysisModelSyncWatermarkQuery(dbContext)
                .ExecuteAsync(int.MaxValue, CancellationToken.None);

            using var scope = new AssertionScope();
            watermark.OverrideCount.Should().Be(0);
            watermark.OverrideMaxId.Should().Be(0);
            watermark.OverrideMaxMutationDate.Should().BeNull();
            watermark.ActivationRuleOverrideCount.Should().Be(0);
            watermark.ActivationRuleOverrideMaxId.Should().Be(0);
            watermark.ActivationRuleOverrideMaxMutationDate.Should().BeNull();
            watermark.ExhaustivePromotedMaxId.Should().Be(0);
            watermark.ExhaustivePromotedMaxCreatedDate.Should().BeNull();
            watermark.NextExpiryDate.Should().BeNull();
        }

        [Fact]
        public async Task TheSameDatabaseStateProducesAnEqualWatermarkSoTheEngineDoesNotRebuildAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
            await GuardSchemaAsync(dbContext);

            var query = new GetEntityAnalysisModelSyncWatermarkQuery(dbContext);
            var first = await query.ExecuteAsync(1, CancellationToken.None);
            var second = await query.ExecuteAsync(1, CancellationToken.None);

            second.Should().Be(first);
        }

        [Fact]
        public async Task AnApiKeyIssuedToAnActiveUserWithATenantRoleMovesTheWatermarkSoTheEngineResynchronisesAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
            await GuardSchemaAsync(dbContext);

            var query = new GetEntityAnalysisModelSyncWatermarkQuery(dbContext);
            var before = await query.ExecuteAsync(1, CancellationToken.None);

            var suffix = Guid.NewGuid().ToString("N");
            var roleId = 0;
            var userId = 0;
            var keyId = 0;

            try
            {
                var roleGuid = Guid.NewGuid();
                roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
                {
                    Guid = roleGuid,
                    Name = "Watermark " + suffix,
                    TenantRegistryId = 1,
                    Deleted = 0,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = "Watermark Test"
                });

                userId = await dbContext.InsertWithInt32IdentityAsync(new UserRegistry
                {
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = roleGuid,
                    Name = "watermark" + suffix,
                    FailedPasswordCount = 0,
                    Active = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = "Watermark Test"
                });

                keyId = await dbContext.InsertWithInt32IdentityAsync(new UserRegistryApiKey
                {
                    Guid = Guid.NewGuid(),
                    UserRegistryId = userId,
                    Name = "watermark",
                    ApiKey = "watermark-" + suffix,
                    ApiKeyDisplay = "watermar",
                    Deleted = 0,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = "Watermark Test"
                });

                var after = await query.ExecuteAsync(1, CancellationToken.None);

                using var scope = new AssertionScope();
                after.ApiKeyCount.Should().Be(before.ApiKeyCount + 1);
                after.Should().NotBe(before);
            }
            finally
            {
                if (keyId != 0)
                {
                    await dbContext.GetTable<UserRegistryApiKey>().Where(w => w.Id == keyId).DeleteAsync();
                }

                if (userId != 0)
                {
                    await dbContext.GetTable<UserRegistry>().Where(w => w.Id == userId).DeleteAsync();
                }

                if (roleId != 0)
                {
                    await dbContext.GetTable<RoleRegistry>().Where(w => w.Id == roleId).DeleteAsync();
                }
            }
        }
    }
}
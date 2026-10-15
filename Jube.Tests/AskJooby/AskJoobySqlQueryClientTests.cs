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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Service.DynamicSql;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class AskJoobySqlQueryClientTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly string connectionString =
            (Environment.GetEnvironmentVariable("JubeTestConnectionString")
             ?? Environment.GetEnvironmentVariable("ConnectionString"))!;

        private readonly List<int> createdConversationIds = [];
        private readonly List<string> createdLogUsers = [];
        private readonly List<int> createdTenantIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            if (createdLogUsers.Count > 0)
            {
                await dbContext.DynamicSqlQueryLog.Where(w => createdLogUsers.Contains(w.CreatedUser))
                    .DeleteAsync();
            }

            if (createdConversationIds.Count > 0)
            {
                await dbContext.AskJoobyConversation.Where(w => createdConversationIds.Contains(w.Id))
                    .DeleteAsync();
            }

            if (createdTenantIds.Count > 0)
            {
                await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
            }
        }

        private string NewUserName([CallerMemberName] string testName = "")
        {
            var userName = $"{DatabaseFixture.Prefix}sqllog-{testName}-{Guid.NewGuid():N}";
            createdLogUsers.Add(userName);

            return userName;
        }

        private async Task<int> SeedConversationAsync(DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N");

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}SqlLogTenant{suffix}",
                Active = 1, Locked = 0, Deleted = 0, Landlord = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var conversationId = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyConversation
            {
                TenantRegistryId = tenantId, Guid = Guid.NewGuid(),
                Title = $"{DatabaseFixture.Prefix}SqlLogConversation", CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix, Version = 1
            });
            createdConversationIds.Add(conversationId);

            return conversationId;
        }

        [Fact]
        public async Task RejectsAQueryReferencingAnUnregisteredTableAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var act = () => client.ExecuteAsync("SELECT * FROM \"TotallyUnregisteredTable\"");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task RejectsANonSelectStatementAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var act = () => client.ExecuteAsync("DELETE FROM \"TotallyUnregisteredTable\"");

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task LogsARejectedQueryWithTheFailureDetailsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var conversationId = await SeedConversationAsync(dbContext);
            var userName = NewUserName();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, userName, _ => true, conversationId);

            var act = () => client.ExecuteAsync("SELECT * FROM \"TotallyUnregisteredTable\"");
            await act.Should().ThrowAsync<InvalidOperationException>();

            var logRows = await dbContext.DynamicSqlQueryLog
                .Where(w => w.AskJoobyConversationId == conversationId).ToListAsync();

            logRows.Should().ContainSingle();
            var logRow = logRows.Single();
            logRow.Succeeded.Should().BeFalse();
            logRow.CreatedUser.Should().Be(userName);
            logRow.RowCount.Should().BeNull();
            logRow.ErrorMessage.Should().Contain("TotallyUnregisteredTable");
            logRow.DurationMs.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task ValidateReportsTheColumnTheDatabaseCannotResolveWithoutExecutingAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var validation = await client.ValidateAsync("SELECT \"NoSuchColumnAtAll\" FROM \"Case\"");

            validation.IsValid.Should().BeFalse();
            validation.Error.Should().Contain("NoSuchColumnAtAll");
            validation.SqlState.Should().Be("42703");
        }

        [Fact]
        public async Task ValidateAcceptsAStatementThePlannerCanResolveAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var validation = await client.ValidateAsync("SELECT \"Id\" FROM \"Case\"");

            validation.IsValid.Should().BeTrue();
            validation.Error.Should().BeNull();
        }

        [Fact]
        public async Task ValidateRefusesAnUnregisteredTableExactlyAsExecutionWouldAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var validation = await client.ValidateAsync("SELECT * FROM \"TotallyUnregisteredTable\"");

            validation.IsValid.Should().BeFalse();
            validation.Error.Should().Contain("TotallyUnregisteredTable");
        }

        [Fact]
        public async Task ExplainReturnsAPlanAndACostWithoutReadingAnyRowAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var plan = await client.ExplainAsync("SELECT \"Id\" FROM \"Case\"");

            plan.PlanLines.Should().NotBeEmpty();

            plan.TotalCost.Should().BeGreaterThan(0,
                "a plan with no cost at all would mean the cost line was not parsed");
        }

        [Fact]
        public async Task TableStatisticsNamesOnlyTheTablesTheStatementActuallyTouchesAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var statistics = await client.TableStatisticsAsync("SELECT \"Id\" FROM \"Case\"");

            statistics.Should().OnlyContain(statistic => statistic.TableName == "Case",
                "the statement references one table, and sizing anything else would be noise");
        }

        [Fact]
        public async Task TableStatisticsSizesNothingForATableTheCallerMayNotQueryAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var statistics = await client.TableStatisticsAsync("SELECT * FROM \"TotallyUnregisteredTable\"");

            statistics.Should().BeEmpty();
        }

        [Fact]
        public async Task NoCallerCanReachARawCatalogueViewThroughDynamicSqlAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            client.ListQueryableTables().Should().NotContain("pg_stat_user_tables");

            var act = () => client.ExecuteAsync("SELECT relname FROM pg_stat_user_tables");
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task QueryHistoryReturnsAggregatesOrSaysThereIsNoneAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var history = await client.QueryHistoryAsync("SELECT \"Id\" FROM \"Case\"");

            if (history.HistoricalDataFound)
            {
                history.Calls.Should().BeGreaterThan(0);
                history.CacheHitPercentage.Should().BeInRange(0, 100);
            }
            else
            {
                history.Calls.Should().Be(0);
            }
        }

        [Fact]
        public async Task LoggingAFailedLookupNeverMasksTheOriginalExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, NewUserName(), _ => true);

            var act = () => client.ExecuteAsync("SELECT * FROM \"TotallyUnregisteredTable\"");

            (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*TotallyUnregisteredTable*");
        }
    }
}
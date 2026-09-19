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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.UserLogout;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.UserLogout;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.UserLogout
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class UserLogoutServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.UserLogout.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<UserLogoutService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return UserLogoutService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private Task<int> TenantOfAsync(DbContext dbContext, string user)
        {
            return dbContext.UserInTenant.Where(u => u.User == user).Select(u => u.TenantRegistryId)
                .FirstAsync();
        }

        private async Task CreateSampleAsync(DbContext dbContext, string createdUser, int? tenantRegistryId,
            int reasonId = 1, int outcomeId = 1, string? message = null, string? cutByUser = null,
            DateTime? createdDate = null, DateTime? sessionStart = null, string? remoteIp = null,
            string? userAgent = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.UserLogout
            {
                CreatedDate = createdDate ?? DateTime.UtcNow,
                CreatedUser = createdUser,
                TenantRegistryId = tenantRegistryId,
                ReasonId = reasonId,
                OutcomeId = outcomeId,
                Message = message,
                CutByUser = cutByUser,
                SessionStartDate = sessionStart,
                RemoteIp = remoteIp,
                UserAgent = userAgent
            }).ConfigureAwait(false);

            createdIds.Add(id);
        }

        private static string NewUser()
        {
            return $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);

            await CreateSampleAsync(dbContext, user, tenant, createdDate: DateTime.UtcNow.AddSeconds(-30));
            await CreateSampleAsync(dbContext, user, tenant, 2, 3, "The tokens could not be revoked.", "Admin");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.CreatedUser == user).ToList();
            mine.Should().HaveCount(2);
            mine[0].Reason.Should().Be("Tokens revoked by an administrator");
            mine[0].Outcome.Should().Be("Revocation failed (cookies cleared)");
            mine[0].CutByUser.Should().Be("Admin");
            mine[0].Message.Should().Be("The tokens could not be revoked.");
            mine[0].TenantRegistryId.Should().Be(tenant);
            mine[1].Reason.Should().Be("User logout");
            mine[1].Outcome.Should().Be("Revoked");
        }

        [Theory]
        [InlineData(1, 1, "User logout", "Revoked")]
        [InlineData(2, 2, "Tokens revoked by an administrator", "No session (cookies cleared only)")]
        [InlineData(3, 3, "Password change", "Revocation failed (cookies cleared)")]
        [InlineData(99, 99, "(unknown)", "(unknown)")]
        public async Task ListDescribesEveryReasonAndOutcomeAsync(int reasonId, int outcomeId, string reason,
            string outcome)
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            await CreateSampleAsync(dbContext, user, await TenantOfAsync(dbContext, fx.Seed.UserWithPermission),
                reasonId, outcomeId);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var row = (await service.ListAsync(search: user)).Rows.Should().ContainSingle().Subject;

            row.ReasonId.Should().Be(reasonId);
            row.Reason.Should().Be(reason);
            row.OutcomeId.Should().Be(outcomeId);
            row.Outcome.Should().Be(outcome);
        }

        [Fact]
        public async Task ListProjectsHowLongTheSessionLivedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            var end = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, user, await TenantOfAsync(dbContext, fx.Seed.UserWithPermission),
                createdDate: end, sessionStart: end.AddMinutes(-12).AddSeconds(-5));

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var row = (await service.ListAsync(search: user)).Rows.Should().ContainSingle().Subject;

            row.SessionSeconds.Should().Be(12 * 60 + 5);
            row.SessionStartDate.Should().BeCloseTo(end.AddMinutes(-12).AddSeconds(-5), TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ListLeavesTheSessionLengthEmptyWhenTheStartIsUnknownAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            await CreateSampleAsync(dbContext, user, await TenantOfAsync(dbContext, fx.Seed.UserWithPermission),
                2, 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var row = (await service.ListAsync(search: user)).Rows.Should().ContainSingle().Subject;

            row.SessionStartDate.Should().BeNull();
            row.SessionSeconds.Should().BeNull();
        }

        [Fact]
        public async Task ListClampsTakeToOneHundredThousandAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.ListAsync(500000);

            result.Rows.Count.Should().BeLessOrEqualTo(100000);
        }

        [Fact]
        public async Task ListWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }

        [Fact]
        public async Task CreateWithNullUserNameThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, null));
        }

        [Fact]
        public async Task CreateWithUnknownTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task ListFiltersByDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, user, tenant, createdDate: oldDate);
            await CreateSampleAsync(dbContext, user, tenant, createdDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.ListAsync(from: newDate.AddMinutes(-1))).Rows.Where(r => r.CreatedUser == user)
                .Should().ContainSingle();
            (await service.ListAsync(from: oldDate.AddDays(-1), to: oldDate.AddDays(1), search: user)).Rows
                .Should().ContainSingle();
        }

        [Fact]
        public async Task ListDefaultsToTheLastHourAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateSampleAsync(dbContext, user, tenant, createdDate: DateTime.UtcNow.AddHours(-3));
            await CreateSampleAsync(dbContext, user, tenant, createdDate: DateTime.UtcNow.AddMinutes(-5));

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.ListAsync(search: user)).Rows.Should().ContainSingle();
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueUser = $"{DatabaseFixture.Prefix}UniqueUser{Guid.NewGuid():N}";
            var otherUser = $"{DatabaseFixture.Prefix}OtherUser{Guid.NewGuid():N}";
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);

            await CreateSampleAsync(dbContext, uniqueUser, tenant);
            await CreateSampleAsync(dbContext, otherUser, tenant);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueUser[..20].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.CreatedUser == uniqueUser || r.CreatedUser == otherUser).ToList();
            mine.Should().ContainSingle();
            mine[0].CreatedUser.Should().Be(uniqueUser);
        }

        [Theory]
        [InlineData("cutby")]
        [InlineData("ip")]
        [InlineData("agent")]
        [InlineData("message")]
        public async Task ListSearchMatchesEveryTextColumnCaseInsensitivelyAsync(string column)
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var marker = $"zzmark{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, NewUser(), tenant,
                message: column == "message" ? marker : null,
                cutByUser: column == "cutby" ? marker : null,
                remoteIp: column == "ip" ? marker : null,
                userAgent: column == "agent" ? marker : null);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.ListAsync(search: marker.ToUpperInvariant())).Rows.Should().ContainSingle();
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateSampleAsync(dbContext, user, tenant);
            await CreateSampleAsync(dbContext, user, tenant);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: user, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateSampleAsync(dbContext, user, tenant);
            await CreateSampleAsync(dbContext, user, tenant);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: user, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsScopedToTheCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var mineUser = NewUser();
            var theirUser = NewUser();
            await CreateSampleAsync(dbContext, mineUser, await TenantOfAsync(dbContext, fx.Seed.UserWithPermission));
            await CreateSampleAsync(dbContext, theirUser, await TenantOfAsync(dbContext, fx.Seed.UserTenantB));

            var tenantAService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantBService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var seenByA = (await tenantAService.ListAsync()).Rows;
            seenByA.Should().Contain(r => r.CreatedUser == mineUser);
            seenByA.Should().NotContain(r => r.CreatedUser == theirUser);

            var seenByB = (await tenantBService.ListAsync()).Rows;
            seenByB.Should().Contain(r => r.CreatedUser == theirUser);
            seenByB.Should().NotContain(r => r.CreatedUser == mineUser);
        }

        [Fact]
        public async Task RowsOfNoTenantAreVisibleToALandlordOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var unknown = NewUser();
            await CreateSampleAsync(dbContext, unknown, null, 1, 2);

            var tenantService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var landlordService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            (await tenantService.ListAsync(search: unknown)).Rows.Should().BeEmpty();
            (await landlordService.ListAsync(search: unknown)).Rows.Should().Contain(r => r.CreatedUser == unknown);
        }

        [Fact]
        public async Task LandlordSeesTheSessionsCutInEveryTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userA = NewUser();
            var userB = NewUser();
            await CreateSampleAsync(dbContext, userA, await TenantOfAsync(dbContext, fx.Seed.UserWithPermission));
            await CreateSampleAsync(dbContext, userB, await TenantOfAsync(dbContext, fx.Seed.UserTenantB));

            var landlordService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var rows = (await landlordService.ListAsync()).Rows;

            rows.Should().Contain(r => r.CreatedUser == userA);
            rows.Should().Contain(r => r.CreatedUser == userB);
        }

        [Fact]
        public async Task ListSortsByCreatedUserAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}Sort{Guid.NewGuid():N}";
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateSampleAsync(dbContext, $"{prefix}A", tenant);
            await CreateSampleAsync(dbContext, $"{prefix}B", tenant);
            await CreateSampleAsync(dbContext, $"{prefix}C", tenant);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: prefix, sortField: "createdUser", sortDirection: "asc");
            ascending.Rows.Select(r => r.CreatedUser).Should().BeInAscendingOrder();

            var descending = await service.ListAsync(search: prefix, sortField: "createdUser",
                sortDirection: "desc");
            descending.Rows.Select(r => r.CreatedUser).Should().BeInDescendingOrder();
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}Sort{Guid.NewGuid():N}";
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateSampleAsync(dbContext, $"{prefix}B", tenant);
            await CreateSampleAsync(dbContext, $"{prefix}A", tenant);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: prefix, sortField: "createdUser",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.CreatedUser).Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}Sort{Guid.NewGuid():N}";
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateSampleAsync(dbContext, $"{prefix}A", tenant);
            await CreateSampleAsync(dbContext, $"{prefix}B", tenant);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: prefix, sortField: "createdUser",
                sortDirection: "garbage");

            result.Rows.Select(r => r.CreatedUser).Should().BeInDescendingOrder();
        }

        [Theory]
        [InlineData("createdDate")]
        [InlineData("createdUser")]
        [InlineData("reasonId")]
        [InlineData("outcomeId")]
        [InlineData("remoteIp")]
        [InlineData("localIp")]
        [InlineData("userAgent")]
        [InlineData("sessionStartDate")]
        [InlineData("cutByUser")]
        [InlineData("message")]
        public async Task ListSortsByEveryDocumentedColumnWithoutErrorAsync(string sortField)
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateSampleAsync(dbContext, user, tenant);
            await CreateSampleAsync(dbContext, user, tenant, 2, 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            foreach (var direction in new[] { "asc", "desc" })
            {
                (await service.ListAsync(search: user, sortField: sortField, sortDirection: direction)).Rows
                    .Should().HaveCount(2);
            }
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var older = DateTime.UtcNow.AddMinutes(-5);
            var newer = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, user, tenant, createdDate: older);
            await CreateSampleAsync(dbContext, user, tenant, createdDate: newer);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: user, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.CreatedUser == user).ToList();
            mine[0].CreatedDate.Should().BeCloseTo(newer, TimeSpan.FromSeconds(1));
            mine[1].CreatedDate.Should().BeCloseTo(older, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateSampleAsync(dbContext, user, tenant);
            await CreateSampleAsync(dbContext, user, tenant);
            await CreateSampleAsync(dbContext, user, tenant);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(1, search: user);

            result.Rows.Should().HaveCount(1);
            result.Total.Should().BeGreaterOrEqualTo(3);
        }

        [Fact]
        public async Task ListStatisticsIsEmptyForThisDtoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = NewUser();
            await CreateSampleAsync(dbContext, user, await TenantOfAsync(dbContext, fx.Seed.UserWithPermission));

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: user);

            result.Statistics.Columns.Should().BeEmpty();
        }

        [Fact]
        public async Task ListEmitsOneAuditLineAndOneSpanAndNoChangeEventAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.ListAsync();

            auditLog.Entries.Should().ContainSingle().Which.Message.Should()
                .Contain("area=UserLogout").And.Contain("op=List").And.Contain("outcome=ok");
            activities.Should().ContainSingle(a => a.OperationName == "UserLogout.List")
                .Which.GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task ForbiddenListIsAuditedAsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, auditLog: auditLog);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());

            auditLog.Entries.Should().ContainSingle().Which.Message.Should().Contain("outcome=forbidden");
        }

        [Fact]
        public void ToolIsDeclaredOnTheServiceMethodAndPublishedByTheCatalogue()
        {
            var attribute = typeof(UserLogoutService).GetMethod(nameof(UserLogoutService.ListAsync))
                .Required().GetCustomAttribute<ServiceOperationAttribute>().Required();
            attribute.Name.Should().Be("UserLogoutList");
            attribute.Kind.Should().Be(OperationKind.Read);
            attribute.Idempotent.Should().BeTrue();

            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("UserLogoutList");
        }
    }
}
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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Exceptions.UserLogin;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.UserLogin;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.UserLogin
{
    using UserLoginService = UserLoginService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class UserLoginServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.UserLogin.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<UserLoginService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return UserLoginService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task CreateSampleAsync(DbContext dbContext, string createdUser, byte failed,
            int? authenticationTypeId = null, int failureTypeId = 0, string? failureMessage = null,
            DateTime? createdDate = null, bool inTenantA = true)
        {
            if (inTenantA && !await dbContext.UserInTenant.AnyAsync(u => u.User == createdUser))
            {
                var tenantAId = await dbContext.UserInTenant.Where(u => u.User == fx.Seed.UserWithPermission)
                    .Select(u => u.TenantRegistryId).FirstAsync();
                await dbContext.InsertAsync(new Data.Poco.UserInTenant
                    { User = createdUser, TenantRegistryId = tenantAId }).ConfigureAwait(false);
            }

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.UserLogin
            {
                CreatedDate = createdDate ?? DateTime.UtcNow,
                CreatedUser = createdUser,
                Failed = failed,
                AuthenticationTypeId = authenticationTypeId,
                FailureTypeId = failureTypeId,
                FailureMessage = failureMessage
            }).ConfigureAwait(false);

            createdIds.Add(id);
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";

            await CreateSampleAsync(dbContext, user, 0, 1);
            await CreateSampleAsync(dbContext, user, 1, 1, 5);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.CreatedUser == user).ToList();
            mine.Should().HaveCount(2);
            mine[0].Failed.Should().Be(1);
            mine[0].FailureReason.Should().Be("Bad credentials (password did not match)");
            mine[1].Failed.Should().Be(0);
            mine[1].FailureReason.Should().BeEmpty();
        }

        [Fact]
        public async Task ListDescribesOAuthFailureWithMessageAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";

            await CreateSampleAsync(dbContext, user, 1, 3, 9, "token expired");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: user);

            result.Rows.Should().ContainSingle();
            result.Rows[0].AuthenticationScheme.Should().Be("OAuth / OpenID Connect");
            result.Rows[0].FailureReason.Should().Be("OAuth: local token validation failed");
            result.Rows[0].FailureMessage.Should().Be("token expired");
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
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, user, 0, 1, createdDate: oldDate);
            await CreateSampleAsync(dbContext, user, 0, 1, createdDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.CreatedUser == user).ToList();
            mine.Should().ContainSingle();
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueUser = $"{DatabaseFixture.Prefix}UniqueUser{Guid.NewGuid():N}";
            var otherUser = $"{DatabaseFixture.Prefix}OtherUser{Guid.NewGuid():N}";

            await CreateSampleAsync(dbContext, uniqueUser, 0, 1);
            await CreateSampleAsync(dbContext, otherUser, 0, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: uniqueUser[..20].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.CreatedUser == uniqueUser || r.CreatedUser == otherUser).ToList();
            mine.Should().ContainSingle();
            mine[0].CreatedUser.Should().Be(uniqueUser);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, user, 0, 1);
            await CreateSampleAsync(dbContext, user, 0, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: user, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, user, 0, 1);
            await CreateSampleAsync(dbContext, user, 0, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: user, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsScopedToTheCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, user, 0, 1);

            var sameTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await sameTenantService.ListAsync(search: user)).Rows.Should().Contain(r => r.CreatedUser == user);
            (await otherTenantService.ListAsync(search: user)).Rows.Should().NotContain(r => r.CreatedUser == user);
        }

        [Fact]
        public async Task AttemptsAgainstUnknownNamesAreVisibleToALandlordOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var unknown = $"{DatabaseFixture.Prefix}Nobody{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, unknown, 1, 1, 1, inTenantA: false);

            var tenantService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var landlordService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            (await tenantService.ListAsync(search: unknown)).Rows.Should().BeEmpty();
            (await landlordService.ListAsync(search: unknown)).Rows.Should().Contain(r => r.CreatedUser == unknown);
        }

        [Fact]
        public async Task LandlordSeesTheLoginAttemptsOfEveryTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, user, 0, 1);

            var landlordService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            (await landlordService.ListAsync(search: user)).Rows.Should().Contain(r => r.CreatedUser == user);
        }

        [Fact]
        public async Task ListSortsByCreatedUserAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var prefix = $"{DatabaseFixture.Prefix}Sort{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, $"{prefix}A", 0, 1);
            await CreateSampleAsync(dbContext, $"{prefix}B", 0, 1);
            await CreateSampleAsync(dbContext, $"{prefix}C", 0, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ascending = await service.ListAsync(search: prefix, sortField: "createdUser",
                sortDirection: "asc");
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
            await CreateSampleAsync(dbContext, $"{prefix}B", 0, 1);
            await CreateSampleAsync(dbContext, $"{prefix}A", 0, 1);

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
            await CreateSampleAsync(dbContext, $"{prefix}A", 0, 1);
            await CreateSampleAsync(dbContext, $"{prefix}B", 0, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: prefix, sortField: "createdUser",
                sortDirection: "garbage");

            result.Rows.Select(r => r.CreatedUser).Should().BeInDescendingOrder();
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";
            var older = DateTime.UtcNow.AddMinutes(-5);
            var newer = DateTime.UtcNow;
            await CreateSampleAsync(dbContext, user, 0, 1, createdDate: older);
            await CreateSampleAsync(dbContext, user, 0, 1, createdDate: newer);

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
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, user, 0, 1);
            await CreateSampleAsync(dbContext, user, 0, 1);
            await CreateSampleAsync(dbContext, user, 0, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(1, search: user);

            result.Rows.Should().HaveCount(1);
            result.Total.Should().BeGreaterOrEqualTo(3);
        }

        [Fact]
        public async Task ListStatisticsIsEmptyForThisDtoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = $"{DatabaseFixture.Prefix}User{Guid.NewGuid():N}";
            await CreateSampleAsync(dbContext, user, 0, 1);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ListAsync(search: user);

            result.Statistics.Columns.Should().BeEmpty();
        }
    }
}
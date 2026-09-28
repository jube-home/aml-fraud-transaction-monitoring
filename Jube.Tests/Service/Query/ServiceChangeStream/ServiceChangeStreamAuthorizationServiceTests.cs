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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Exceptions.Query.ServiceChangeStream;
using Jube.Service.Query.ServiceChangeStream;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.ServiceChangeStream
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ServiceChangeStreamAuthorizationServiceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static Task<ServiceChangeStreamAuthorizationService> BuildServiceAsync(DbContext dbContext,
            string? userName, ILog? log = null)
        {
            return ServiceChangeStreamAuthorizationService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers);
        }

        private static Task<int> TenantOfAsync(DbContext dbContext, string userName)
        {
            return dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId)
                .FirstAsync();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                ServiceChangeStreamAuthorizationService.CreateAsync(dbContext, userName, log, localizers));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task TenantlessUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant, log));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task AUserWithNoFeaturePermissionIsStillPermittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var expectedTenant = await TenantOfAsync(dbContext, fx.Seed.UserWithoutPermission);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            service.UserName.Should().Be(fx.Seed.UserWithoutPermission);
            service.TenantRegistryId.Should().Be(expectedTenant);
        }

        [Fact]
        public async Task PermittedUserResolvesToTheirOwnTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var expectedTenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            service.UserName.Should().Be(fx.Seed.UserWithPermission);
            service.TenantRegistryId.Should().Be(expectedTenant);
        }

        [Fact]
        public async Task DifferentTenantsResolveToDifferentTenantIdsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            tenantA.Should().NotBe(tenantB);
            serviceA.TenantRegistryId.Should().Be(tenantA);
            serviceB.TenantRegistryId.Should().Be(tenantB);
        }
    }
}
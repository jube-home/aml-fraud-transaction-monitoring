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

using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Exceptions.Query.GridModel;
using Jube.Service.Query.GridModel;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.GridModel
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GridModelAuthorizationServiceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static Task<GridModelAuthorizationService> BuildServiceAsync(DbContext dbContext, string? userName,
            ILog? log = null)
        {
            return GridModelAuthorizationService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers);
        }

        [Fact]
        public void MonitoringAreasAreLandlordOnlyAndTheLoginAreasNeedTheUserPermission()
        {
            GridModelAccess.Resolve("HttpProcessingCounter").LandlordOnly.Should().BeTrue();
            GridModelAccess.Resolve("PostgresActivity").LandlordOnly.Should().BeTrue();
            GridModelAccess.Resolve("UserLogin").Should().BeEquivalentTo((false, new[] { 35 }));
            GridModelAccess.Resolve("userlogout").Should().BeEquivalentTo((false, new[] { 35 }));
            GridModelAccess.Resolve("NoSuchArea").LandlordOnly.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, userName));
        }

        [Fact]
        public async Task UnknownAndTenantlessUsersThrowNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task AUserWithoutPermissionIsRefusedEveryAreaWithALocalisedMessageAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = Assert.Throws<ForbiddenException>(() => service.EnsurePermitted("UserLogin"));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([35]);
            ex.Message.Should().NotBeNullOrWhiteSpace();
            service.Permits("UserLogin").Should().BeFalse();
            service.Permits("HttpProcessingCounter").Should().BeFalse();
        }

        [Fact]
        public async Task AUserHoldingPermission35SeesTheLoginAreasButNotTheLandlordOnlyOnesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            service.Permits("UserLogin").Should().BeTrue();
            service.Permits("UserLogout").Should().BeTrue();
            service.Permits("HttpProcessingCounter").Should().BeFalse();
            Assert.Throws<ForbiddenException>(() => service.EnsurePermitted("PostgresActivity"));
        }

        [Fact]
        public async Task ALandlordSeesEveryAreaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            service.Permits("HttpProcessingCounter").Should().BeTrue();
            service.Permits("UserLogin").Should().BeTrue();
        }
    }
}

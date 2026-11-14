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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Exceptions.Query.Svirfd;
using Jube.Service.Query.Svirfd;
using Jube.Svirfd;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Svirfd;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SvirfdServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> roleIds = [];
        private readonly List<string> userNames = [];
        private string permitted = string.Empty;
        private string refused = string.Empty;
        private int tenantId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            tenantId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(w => w.TenantRegistryId).FirstAsync();
            permitted = await SeedAsync(dbContext, "Allowed", [72]);
            refused = await SeedAsync(dbContext, "Denied", [1]);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.UserInTenant.Where(w => userNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => userNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && roleIds.Contains(w.RoleRegistryId.Value)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => roleIds.Contains(w.Id)).DeleteAsync();
        }

        [Fact]
        public async Task LooksUpAnAddressWithTheRecordNarrativeAndCreditAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await using var host = await SvirfdFixture.StartDirectoryHostAsync();
            var service = await SvirfdService.CreateAsync(dbContext, permitted, TestLog.NoOp, host, localizers);

            var result = await service.LookupAsync("37.30.56.54");

            result.Ip.Should().Be("37.30.56.54");
            result.Mode.Should().Be("Directory");
            result.Headline.Should().Contain("T-Mobile");
            result.Narrative.Should().NotBeEmpty();
            result.Narrative[0].Id.Should().Be("identity");
            result.Attribution.Should().NotBeNullOrWhiteSpace();
            SvirfdJson.Deserialize(result.RecordJson)!.Asn!.Asn.Should().Be("AS12912");

            var status = service.Status();
            status.Enabled.Should().BeTrue();
            status.Ready.Should().BeTrue();
            status.Mode.Should().Be("Directory");
        }

        [Fact]
        public async Task RefusesWithoutThePermissionAndOnBadInputAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await using var host = await SvirfdFixture.StartDirectoryHostAsync();

            var denied = await SvirfdService.CreateAsync(dbContext, refused, TestLog.NoOp, host, localizers);
            await FluentActions.Awaiting(() => denied.LookupAsync("37.30.56.54"))
                .Should().ThrowAsync<ForbiddenException>();

            var service = await SvirfdService.CreateAsync(dbContext, permitted, TestLog.NoOp, host, localizers);
            await FluentActions.Awaiting(() => service.LookupAsync("123.456.789.200"))
                .Should().ThrowAsync<InvalidAddressException>();
            await FluentActions.Awaiting(() => service.LookupAsync(null)).Should()
                .ThrowAsync<InvalidAddressException>();

            await FluentActions.Awaiting(() => SvirfdService.CreateAsync(dbContext, null, TestLog.NoOp, host, localizers))
                .Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task ADisabledInstallationAnswersNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await SvirfdService.CreateAsync(dbContext, permitted, TestLog.NoOp, new SvirfdNullLookup(), localizers);

            await FluentActions.Awaiting(() => service.LookupAsync("37.30.56.54", CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
            service.Status().Enabled.Should().BeFalse();
        }

        [Fact]
        public async Task ATenantWithRemoteLookupsSwitchedOffIsRefusedInRemoteModeAndNothingIsSentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var handler = new StubSvirfdHandler((_, _) => throw new InvalidOperationException("nothing may be sent"));
            var settings = new SvirfdSettings { Mode = SvirfdMode.Remote, BaseUrl = "https://svirfd.test" };
            await using var host = new SvirfdHost(settings, new System.Net.Http.HttpClient(handler),
                NullLogger.Instance);
            await host.StartAsync(CancellationToken.None);

            await SetTenantSwitchAsync(dbContext, 0);

            try
            {
                var service = await SvirfdService.CreateAsync(dbContext, permitted, TestLog.NoOp, host, localizers);

                await FluentActions.Awaiting(() => service.LookupAsync("8.8.8.8"))
                    .Should().ThrowAsync<NotFoundException>();

                handler.Calls.Should().Be(0);
                service.Status().TenantRemoteLookupsEnabled.Should().BeFalse();
            }
            finally
            {
                await SetTenantSwitchAsync(dbContext, 1);
            }

            var restored = await SvirfdService.CreateAsync(dbContext, permitted, TestLog.NoOp, host, localizers);
            restored.Status().TenantRemoteLookupsEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task ATenantWithRemoteLookupsSwitchedOffStillGetsDirectoryLookupsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await using var host = await SvirfdFixture.StartDirectoryHostAsync();

            await SetTenantSwitchAsync(dbContext, 0);

            try
            {
                var service = await SvirfdService.CreateAsync(dbContext, permitted, TestLog.NoOp, host, localizers);

                (await service.LookupAsync("37.30.56.54")).Ip.Should().Be("37.30.56.54");
                service.Status().TenantRemoteLookupsEnabled.Should().BeFalse();
            }
            finally
            {
                await SetTenantSwitchAsync(dbContext, 1);
            }
        }

        private Task<int> SetTenantSwitchAsync(DbContext dbContext, byte value)
        {
            return dbContext.TenantRegistry.Where(t => t.Id == tenantId)
                .Set(t => t.SvirfdRemoteLookupsEnabled, (byte?)value).UpdateAsync();
        }

        [Fact]
        public async Task RefusalMessagesComeFromTheLocaliserInTheCallersCultureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var previous = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("en");
                var service = await SvirfdService.CreateAsync(dbContext, permitted, TestLog.NoOp,
                    new SvirfdNullLookup(), localizers);
                var english = (await FluentActions.Awaiting(() => service.LookupAsync("37.30.56.54"))
                    .Should().ThrowAsync<NotFoundException>()).Which.Message;
                english.Should().Be("Svirfd is disabled.");

                foreach (var culture in new[] { "pl", "ar", "ka", "id", "sv", "ur", "hi", "fr" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(culture);
                    var localised = await SvirfdService.CreateAsync(dbContext, permitted, TestLog.NoOp,
                        new SvirfdNullLookup(), localizers);
                    var message = (await FluentActions.Awaiting(() => localised.LookupAsync("37.30.56.54"))
                        .Should().ThrowAsync<NotFoundException>()).Which.Message;
                    message.Should().NotBe(english, culture);
                    message.Should().Contain("Svirfd");
                }

                CultureInfo.CurrentUICulture = new CultureInfo("pl");
                var denied = await SvirfdService.CreateAsync(dbContext, refused, TestLog.NoOp,
                    new SvirfdNullLookup(), localizers);
                (await FluentActions.Awaiting(() => denied.LookupAsync("37.30.56.54"))
                    .Should().ThrowAsync<ForbiddenException>()).Which.Message.Should().Be("Brak uprawnień.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = previous;
            }
        }

        private async Task<string> SeedAsync(DbContext dbContext, string label, int[] specs)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}RoleSvirfd{label}{suffix}", Active = 1, Locked = 0,
                Deleted = 0, TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            roleIds.Add(roleId);

            foreach (var spec in specs)
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), PermissionSpecificationId = spec, RoleRegistryId = roleId, Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });
            }

            var userName = $"{DatabaseFixture.Prefix}UserSvirfd{label}{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = userName,
                Email = $"{userName}@example.invalid", Password = "not-used-by-permission-checks", Active = 1,
                PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = userName, TenantRegistryId = tenantId });
            userNames.Add(userName);

            return userName;
        }
    }
}

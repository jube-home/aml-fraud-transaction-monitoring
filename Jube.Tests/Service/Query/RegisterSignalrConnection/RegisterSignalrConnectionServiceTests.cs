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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Agent;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.RegisterSignalrConnection;
using Jube.Service.Observability;
using Jube.Service.Query.RegisterSignalrConnection;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.RegisterSignalrConnection.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.RegisterSignalrConnection
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RegisterSignalrConnectionServiceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static Task<RegisterSignalrConnectionService> BuildServiceAsync(DbContext dbContext,
            string? userName, ISignalrGroupRegistrar registrar, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return RegisterSignalrConnectionService.CreateAsync(dbContext, userName, registrar,
                log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
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
                RegisterSignalrConnectionService.CreateAsync(dbContext, userName, new CapturingRegistrar(), log,
                    localizers, new NullServiceChangeBus(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser, new CapturingRegistrar()));
        }

        [Fact]
        public async Task TenantlessUserIsRefusedAndNeverAddedToTenantZeroGroupAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var registrar = new CapturingRegistrar();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant, registrar));

            registrar.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task MissingPermissionThrowsForbiddenAndDoesNotRegisterAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var registrar = new CapturingRegistrar();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, registrar, log);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.RegisterAsync("conn-1"));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([30]);
            registrar.Calls.Should().BeEmpty();
            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task PermittedUserIsAddedToTheirTenantGroupAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var registrar = new CapturingRegistrar();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, registrar);
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);

            await service.RegisterAsync("conn-abc");

            registrar.Calls.Should().ContainSingle().Which.Should().Be(("conn-abc", "Tenant_" + tenant));
        }

        [Fact]
        public async Task DifferentTenantsGetDifferentGroupsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var registrar = new CapturingRegistrar();
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, registrar);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB, registrar);
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);

            await serviceA.RegisterAsync("a-conn");
            await serviceB.RegisterAsync("b-conn");

            tenantA.Should().NotBe(tenantB);
            registrar.Calls.Should().Equal(("a-conn", "Tenant_" + tenantA), ("b-conn", "Tenant_" + tenantB));
        }

        [Theory]
        [InlineData("")]
        [InlineData("has space")]
        [InlineData("semi;colon")]
        [InlineData("../../etc")]
        [InlineData("new\nline")]
        public async Task MalformedConnectionIdIsRejectedAsync(string connectionId)
        {
            await using var dbContext = fx.GetDbContext();
            var registrar = new CapturingRegistrar();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, registrar);

            var ex = await Assert.ThrowsAsync<InvalidConnectionIdException>(() => service.RegisterAsync(connectionId));

            ex.Code.Should().Be("InvalidConnectionId");
            registrar.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task OverlongConnectionIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var registrar = new CapturingRegistrar();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, registrar);

            await Assert.ThrowsAsync<InvalidConnectionIdException>(() => service.RegisterAsync(new string('a', 129)));
            await service.RegisterAsync(new string('a', 128));

            registrar.Calls.Should().ContainSingle();
        }

        [Fact]
        public async Task RealisticSignalrConnectionIdIsAcceptedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var registrar = new CapturingRegistrar();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, registrar);

            await service.RegisterAsync("aB3_-xYz09kLmNoPqRsTuV");

            registrar.Calls.Should().ContainSingle();
        }

        [Fact]
        public async Task ConnectionOwnedByAnotherUserIsRefusedAndNotAddedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var registrar = new CapturingRegistrar
            {
                OwnEverything = false,
                Owners =
                {
                    ["victim-conn"] = fx.Seed.UserTenantB,
                    ["mine-conn"] = fx.Seed.UserWithPermission
                }
            };
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, registrar, log);

            var ex = await Assert.ThrowsAsync<ConnectionNotOwnedException>(() => service.RegisterAsync("victim-conn"));
            await Assert.ThrowsAsync<ConnectionNotOwnedException>(() => service.RegisterAsync("unknown-conn"));
            await service.RegisterAsync("mine-conn");

            ex.Code.Should().Be("ConnectionNotOwned");
            registrar.Calls.Should().ContainSingle().Which.ConnectionId.Should().Be("mine-conn");
            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains("victim-conn"));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task PermissionIsCheckedBeforeConnectionIdSoNothingLeaksAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, new CapturingRegistrar());

            await Assert.ThrowsAsync<ForbiddenException>(() => service.RegisterAsync("bad id!"));
        }

        [Fact]
        public async Task StaleTenantGroupsAreRemovedBeforeJoiningCurrentTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var current = "Tenant_" + tenant;
            var registrar = new CapturingRegistrar
            {
                Joined =
                {
                    ["c1"] = ["Tenant_999999", current]
                }
            };
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, registrar);

            await service.RegisterAsync("c1");

            registrar.Removals.Should().ContainSingle().Which.Should().Be(("c1", "Tenant_999999"));
            registrar.Calls.Should().ContainSingle().Which.Should().Be(("c1", current));
        }

        [Fact]
        public void TenantGroupNamingIsCentralAndFailsClosed()
        {
            TenantGroup.Name(7).Should().Be("Tenant_7");
            TenantGroup.TryName(7).Should().Be("Tenant_7");
            TenantGroup.TryName("7").Should().Be("Tenant_7");
            TenantGroup.TryName(0).Should().BeNull();
            TenantGroup.TryName(-1).Should().BeNull();
            TenantGroup.TryName((int?)null).Should().BeNull();
            TenantGroup.TryName("0").Should().BeNull();
            TenantGroup.TryName("abc").Should().BeNull();
            TenantGroup.TryName("").Should().BeNull();
            TenantGroup.TryName((string?)null).Should().BeNull();
            TenantGroup.TryName("-3").Should().BeNull();
            Assert.Throws<ArgumentOutOfRangeException>(() => TenantGroup.Name(0));
            TenantGroup.IsTenantGroup("Tenant_5").Should().BeTrue();
            TenantGroup.IsTenantGroup("Other").Should().BeFalse();
        }

        [Fact]
        public void GroupNameFormatIsTenantUnderscoreId()
        {
            RegisterSignalrConnectionService.GroupName(42).Should().Be("Tenant_42");
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAndDoesNotRegisterAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var registrar = new CapturingRegistrar();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, registrar);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RegisterAsync("c", cts.Token));

            registrar.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task HubFailureLogsErrorAndPropagatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                new CapturingRegistrar { Throw = true }, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync("c"));

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanAndOneAuditLineAndNoChangeEventAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, new CapturingRegistrar(),
                auditLog: auditLog, serviceChangeBus: bus);

            await service.RegisterAsync("c");

            activities.Should().ContainSingle(a => a.OperationName == "RegisterSignalrConnection.Register")
                .Subject.GetTagItem("jube.outcome").Should().Be("ok");
            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Register");
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniqueWriteToolName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            ServiceToolCatalogue.All.Should().ContainSingle(t => t.Name == "RegisterSignalrConnectionRegister")
                .Which.Kind.Should().Be(OperationKind.Write);
        }
    }
}
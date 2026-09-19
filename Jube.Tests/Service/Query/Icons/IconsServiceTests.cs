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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Query.Icons;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.Icons;
using Jube.Service.Observability;
using Jube.Service.Query.Icons;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.Icons.Models;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.Icons
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class IconsServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<string> tempDirectories = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public Task DisposeAsync()
        {
            foreach (var directory in tempDirectories.Where(Directory.Exists))
            {
                Directory.Delete(directory, true);
            }

            return Task.CompletedTask;
        }

        private string CreateWebRoot(params string[] iconFiles)
        {
            var webRoot = Path.Combine(Path.GetTempPath(), $"{DatabaseFixture.Prefix}IconsWebRoot{Guid.NewGuid():N}");
            var icons = Path.Combine(webRoot, "icons");
            Directory.CreateDirectory(icons);
            tempDirectories.Add(webRoot);
            foreach (var file in iconFiles)
            {
                File.WriteAllText(Path.Combine(icons, file), "x");
            }

            return webRoot;
        }

        private static Task<IconsService> BuildServiceAsync(DbContext dbContext, string? userName,
            IIconFileSource? source = null, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return IconsService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), source ?? new FakeIconFileSource(),
                auditLog ?? TestLog.NoOp);
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
                IconsService.CreateAsync(dbContext, userName, log, localizers, new NullServiceChangeBus(),
                    new FakeIconFileSource(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task MissingPermissionThrowsForbiddenWithCodeAndRequiredSpecificationsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                new FakeIconFileSource("a.png"));

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([24]);
        }

        [Fact]
        public async Task ForbiddenDoesNotTouchTheFileSystemAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                new ThrowingIconFileSource());

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
        }

        [Fact]
        public async Task ReturnsOneDtoPerFileNameInSourceOrderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                new FakeIconFileSource("zeta.png", "alpha.svg", "Mid.gif"));

            var result = await service.GetAsync();

            result.Select(r => r.Name).Should().Equal("zeta.png", "alpha.svg", "Mid.gif");
        }

        [Fact]
        public async Task EmptySourceReturnsEmptyListAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task WebRootSourceListsOnlyFilesOfIconsDirectoryAsNamesAsync()
        {
            var webRoot = CreateWebRoot("a.png", "b.svg", "c.ico");
            Directory.CreateDirectory(Path.Combine(webRoot, "icons", "nested"));
            await File.WriteAllTextAsync(Path.Combine(webRoot, "outside.png"), "x");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                new WebRootIconFileSource(webRoot));

            var result = await service.GetAsync();

            result.Should().AllBeOfType<IconDto>();
            result.Select(r => r.Name).Should().BeEquivalentTo("a.png", "b.svg", "c.ico");
            result.Select(r => r.Name).Should().Equal(
                Directory.GetFiles(Path.Combine(webRoot, "icons")).Select(f => Path.GetFileName(f)));
        }

        [Fact]
        public async Task WebRootSourceWithMissingIconsDirectoryPropagatesAndLogsErrorAsync()
        {
            var webRoot = Path.Combine(Path.GetTempPath(), $"{DatabaseFixture.Prefix}IconsMissing{Guid.NewGuid():N}");
            var log = new TestLog();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                new WebRootIconFileSource(webRoot), log);

            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => service.GetAsync());

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task ResultIsTheSameForTenantAAndTenantBUsersBecauseIconsAreNotTenantDataAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var source = new FakeIconFileSource("one.png", "two.png");
            var a = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, source);
            var b = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB, source);

            var resultA = (await a.GetAsync()).Select(r => r.Name).ToList();
            var resultB = (await b.GetAsync()).Select(r => r.Name).ToList();

            resultB.Should().Equal(resultA);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log: log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task EachCallEmitsOneSpanWithOutcomeTagAndExactlyOneAuditLineAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                new FakeIconFileSource("a.png"), auditLog: auditLog);
            await service.GetAsync();

            activities.Should().ContainSingle(a => a.OperationName == "Icons.Get")
                .Subject.GetTagItem("jube.outcome").Should().Be("ok");
            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetAsync();
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync());

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniqueToolName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("IconsGet");
        }
    }
}
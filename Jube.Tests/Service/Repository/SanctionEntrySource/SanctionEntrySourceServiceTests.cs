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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Exceptions.Repository.SanctionEntrySource;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using SanctionEntrySourceService = Jube.Service.Repository.SanctionEntrySource.SanctionEntrySourceService;

namespace Jube.Test.Service.Repository.SanctionEntrySource
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SanctionEntrySourceServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdSourceIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.SanctionEntryRejection.Where(w => createdSourceIds.Contains(w.SanctionEntrySourceId ?? 0))
                .DeleteAsync();
            await dbContext.SanctionEntry.Where(w => createdSourceIds.Contains(w.SanctionEntrySourceId ?? 0))
                .DeleteAsync();
            await dbContext.SanctionEntryImport
                .Where(w => createdSourceIds.Contains(w.SanctionEntrySourceId ?? 0)).DeleteAsync();
            await dbContext.SanctionEntrySource.Where(w => createdSourceIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<SanctionEntrySourceService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return SanctionEntrySourceService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateSourceAsync(DbContext dbContext, char? delimiter = ',',
            string multiPartStringIndex = "0", int? referenceIndex = 1, byte skip = 1)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.SanctionEntrySource
            {
                Name = $"{DatabaseFixture.Prefix}Source{Guid.NewGuid():N}"[..40],
                Severity = 1,
                Delimiter = delimiter,
                MultiPartStringIndex = multiPartStringIndex,
                ReferenceIndex = (byte?)referenceIndex,
                Skip = skip
            }).ConfigureAwait(false);

            createdSourceIds.Add(id);
            return id;
        }

        private static (MemoryStream Stream, long Length) BuildCsv(string csv)
        {
            var bytes = Encoding.UTF8.GetBytes(csv);
            return (new MemoryStream(bytes), bytes.Length);
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
        public async Task GetWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
        }

        [Fact]
        public async Task GetAsLandlordIncludesTheConfiguredSourceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var sourceId = await CreateSourceAsync(dbContext);
            var expectedName = await dbContext.SanctionEntrySource.Where(w => w.Id == sourceId)
                .Select(s => s.Name).SingleAsync().ConfigureAwait(false);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.GetAsync();

            result.Should().Contain(r => r.Id == sourceId && r.Name == expectedName);
        }

        [Fact]
        public async Task ListWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }

        [Fact]
        public async Task ListClampsTakeToTwoHundredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var result = await service.ListAsync(100000);

            result.Items.Count.Should().BeLessOrEqualTo(200);
        }

        [Fact]
        public async Task ListWithAfterIdExcludesThatRowAndIncludesLaterOnesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var sourceAId = await CreateSourceAsync(dbContext);
            var sourceBId = await CreateSourceAsync(dbContext);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(200, sourceAId);

            result.Items.Should().NotContain(r => r.Id == sourceAId);
            result.Items.Should().Contain(r => r.Id == sourceBId);
        }

        [Fact]
        public async Task ImportWithoutLandlordThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ImportAsync(null, 0, 0));
        }

        [Fact]
        public async Task ImportWithNoFileThrowsDtoValidationExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var sourceId = await CreateSourceAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var exception = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.ImportAsync(null, 0, sourceId));

            exception.Result.Errors.Should().Contain(e => e.PropertyName == "files");
        }

        [Fact]
        public async Task ImportWithEmptyFileThrowsDtoValidationExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var sourceId = await CreateSourceAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var (stream, _) = BuildCsv(string.Empty);

            await using (stream)
            {
                var exception = await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.ImportAsync(stream, 0, sourceId));

                exception.Result.Errors.Should().Contain(e => e.PropertyName == "files");
            }
        }

        [Fact]
        public async Task ImportWithUnknownSanctionEntrySourceIdThrowsDtoValidationExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var (stream, length) = BuildCsv("Name,Reference\r\nRobert Mugabe,REF001\r\n");

            await using (stream)
            {
                var exception = await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.ImportAsync(stream, length, int.MaxValue));

                exception.Result.Errors.Should().Contain(e => e.PropertyName == "sanctionEntrySourceId");
            }
        }

        [Fact]
        public async Task ImportWithNoDelimiterConfiguredSkipsSilentlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var sourceId = await CreateSourceAsync(dbContext, null);
            var capturingBus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, serviceChangeBus: capturingBus);
            var (stream, length) = BuildCsv("Name,Reference\r\nRobert Mugabe,REF001\r\n");

            await using (stream)
            {
                await service.ImportAsync(stream, length, sourceId);
            }

            var imports = await dbContext.SanctionEntryImport.Where(w => w.SanctionEntrySourceId == sourceId)
                .ToListAsync();
            imports.Should().BeEmpty("a Sanction Entry Source with no Delimiter configured is a documented " +
                                     "no-op, preserved verbatim from the legacy controller");
            capturingBus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportInsertsEntryAndRecordsSuccessfulImportRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var sourceId = await CreateSourceAsync(dbContext);
            var capturingBus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, serviceChangeBus: capturingBus);
            var (stream, length) = BuildCsv("Name,Reference\r\nRobert Mugabe,REF001\r\n");

            await using (stream)
            {
                await service.ImportAsync(stream, length, sourceId);
            }

            var entries = await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId == sourceId).ToListAsync();
            entries.Should().ContainSingle();
            entries[0].SanctionEntryElementValue.Should().Be("Robert Mugabe");
            entries[0].SanctionEntryReference.Should().Be("REF001");
            entries[0].CreatedUser.Should().Be(fx.Seed.LandlordUser);

            var imports = await dbContext.SanctionEntryImport.Where(w => w.SanctionEntrySourceId == sourceId)
                .ToListAsync();
            imports.Should().ContainSingle();
            imports[0].Successful.Should().Be(1);
            imports[0].TotalRows.Should().Be(1);
            imports[0].InsertedCount.Should().Be(1);
            imports[0].RevivedCount.Should().Be(0);
            imports[0].UnchangedCount.Should().Be(0);
            imports[0].RejectedCount.Should().Be(0);

            capturingBus.Published.Should().ContainSingle();
            capturingBus.Published[0].Area.Should().Be("SanctionEntrySource");
            capturingBus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            capturingBus.Published[0].EntityId.Should().Be(imports[0].Id);
        }

        [Fact]
        public async Task ImportDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var sourceId = await CreateSourceAsync(dbContext);
            var capturingBus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, serviceChangeBus: capturingBus);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.ImportAsync(null, 0, sourceId));

            capturingBus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task PermissionDeniedIsLoggedAsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task GetWritesExactlyOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle(e => e.Message.Contains("op=List"));
        }
    }
}
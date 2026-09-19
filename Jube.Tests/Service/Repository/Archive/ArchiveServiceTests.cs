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
using Jube.Data.Repository;
using Jube.Dto.Repository.Archive;
using Jube.Service.Exceptions.Repository.Archive;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.Archive;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Repository.Archive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ArchiveServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<long> createdArchiveIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdArchiveIds)
            {
                var entryGuid = await dbContext.Archive.Where(a => a.Id == id)
                    .Select(a => (Guid?)a.EntityAnalysisModelInstanceEntryGuid).FirstOrDefaultAsync();
                await dbContext.GetTable<Data.Poco.ArchiveTag>()
                    .Where(w => w.EntityAnalysisModelInstanceEntryGuid == entryGuid)
                    .DeleteAsync();
                await dbContext.Archive.Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                    .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ArchiveService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ArchiveService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext, string createdUser)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            }).ConfigureAwait(false);

            createdModelIds.Add(saved.Id);
            return saved.Id;
        }

        private async Task<(long ArchiveId, Guid Guid)> CreateArchiveAsync(DbContext dbContext, int modelId,
            object[]? initialTags = null)
        {
            var guid = Guid.NewGuid();
            var json = new JObject
            {
                ["tag"] = new JArray(initialTags ?? [])
            }.ToString(Newtonsoft.Json.Formatting.None);

            var id = await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json,
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = modelId,
                CreatedDate = DateTime.UtcNow
            }).ConfigureAwait(false);

            createdArchiveIds.Add(id);
            return (id, guid);
        }

        [Fact]
        public async Task TagReplacesTagSetOnArchiveJsonAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var (_, guid) = await CreateArchiveAsync(dbContext, modelId, ["Old"]);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = guid,
                Tag = ["New", "Second"]
            });

            var archive = await dbContext.Archive.FirstAsync(w => w.EntityAnalysisModelInstanceEntryGuid == guid);
            var tags = JObject.Parse(archive.Json)["tag"].Required().Select(t => t.ToString()).ToList();
            tags.Should().BeEquivalentTo(["New", "Second"]);
        }

        [Fact]
        public async Task TagMergesArchiveTagRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var (_, guid) = await CreateArchiveAsync(dbContext, modelId);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = guid,
                Tag = ["Alpha", "Beta"]
            });

            var archiveTags = await dbContext.GetTable<Data.Poco.ArchiveTag>()
                .Where(w => w.EntityAnalysisModelInstanceEntryGuid == guid && (w.Deleted == null || w.Deleted == 0))
                .ToListAsync();

            archiveTags.Select(t => t.Name).Should().BeEquivalentTo(["Alpha", "Beta"]);
        }

        [Fact]
        public async Task TagWithEmptyArrayClearsAllTagsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var (_, guid) = await CreateArchiveAsync(dbContext, modelId, ["Existing"]);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = guid,
                Tag = []
            });

            var archive = await dbContext.Archive.FirstAsync(w => w.EntityAnalysisModelInstanceEntryGuid == guid);
            var tags = JObject.Parse(archive.Json)["tag"].Required().Select(t => t.ToString()).ToList();
            tags.Should().BeEmpty();
        }

        [Fact]
        public async Task TagIsNotVisibleAcrossTenantsThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var (_, guid) = await CreateArchiveAsync(dbContext, modelId);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = guid,
                Tag = ["Should", "Not", "Apply"]
            }));

            var archive = await dbContext.Archive.FirstAsync(w => w.EntityAnalysisModelInstanceEntryGuid == guid);
            JObject.Parse(archive.Json)["tag"].Required().Should().BeEmpty();
        }

        [Fact]
        public async Task TagWithUnknownGuidThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                Tag = ["X"]
            }));
        }

        [Fact]
        public async Task TagWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var (_, guid) = await CreateArchiveAsync(dbContext, modelId);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = guid,
                Tag = ["X"]
            }));
        }

        [Fact]
        public async Task TagWithEmptyGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.Empty,
                Tag = ["X"]
            }));

            ex.Result.Errors.Should()
                .Contain(e => e.PropertyName == nameof(ArchiveTagDto.EntityAnalysisModelInstanceEntryGuid));
        }

        [Fact]
        public async Task TagWithNullTagArrayThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                Tag = null
            }));

            ex.Result.Errors.Should().Contain(e => e.PropertyName == nameof(ArchiveTagDto.Tag));
        }

        [Fact]
        public async Task TagWithBlankTagNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                Tag = ["Valid", "  "]
            }));

            ex.Result.Errors.Should().Contain(e => e.PropertyName == "Tag[1]");
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
        public async Task TagPublishesUpdatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var (archiveId, guid) = await CreateArchiveAsync(dbContext, modelId);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = guid,
                Tag = ["X"]
            });

            bus.Published.Should().ContainSingle();
            var change = bus.Published.Single();
            change.Area.Should().Be("Archive");
            change.Kind.Should().Be(ServiceChangeKind.Updated);
            change.EntityId.Should().Be((int)archiveId);
        }

        [Fact]
        public async Task TagDoesNotPublishAChangeEventOnFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await Assert.ThrowsAsync<NotFoundException>(() => service.TagAsync(new ArchiveTagDto
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                Tag = ["X"]
            }));

            bus.Published.Should().BeEmpty();
        }
    }
}
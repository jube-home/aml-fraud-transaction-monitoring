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
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseNote;
using Jube.Service.Exceptions.Repository.CaseNote;
using Jube.Service.Query.CaseNoteByCaseKeyValue;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseNote;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Repository.CaseNote
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseNoteExtensionServiceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly EngineJsonSerializationHelper jsonSerializationHelper = new();

        private JubeDynamicEnvironment Environment() => TestDynamicEnvironment.Create(
            new Dictionary<string, string> { ["ConnectionString"] = fx.ConnectionString });

        private async Task<CaseNoteService> ServiceAsync(Jube.Data.Context.DbContext dbContext, string? user)
        {
            return await CaseNoteService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), Environment(), jsonSerializationHelper, TestLog.NoOp);
        }

        private CaseNoteDto Draft(CaseNoteSeed seed, string text = "analyst note") => new()
        {
            Note = text, ActionId = seed.ActionId, PriorityId = 2, CaseId = seed.CaseId, CaseKey = seed.CaseKey,
            CaseKeyValue = seed.CaseKeyValue, Payload = "{}"
        };

        [Fact]
        public async Task ACallerCannotForgeTheSourceKindOrStateOfANoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var draft = Draft(seed);
            draft.SourceId = 3;
            draft.KindId = 2;
            draft.Version = 9;

            var saved = await service.InsertAsync(draft);
            var stored = await dbContext.CaseNote.FirstAsync(w => w.Id == saved.Id);

            stored.SourceId.Should().Be(1);
            stored.KindId.Should().Be(1);
            stored.Version.Should().Be(1);
            stored.SupersedesNoteId.Should().BeNull();
            stored.FactsHash.Should().BeNull();
        }

        [Fact]
        public async Task TheAuthorCanMarkTheirOwnNoteAsAFindingAndClearItAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(Draft(seed));

            var marked = await service.SetFindingAsync(new CaseNoteFindingRequestDto { Id = saved.Id, IsFinding = true });
            marked.IsFinding.Should().BeTrue();
            (await dbContext.CaseNote.FirstAsync(w => w.Id == saved.Id)).IsFinding.Should().BeTrue();

            await service.SetFindingAsync(new CaseNoteFindingRequestDto { Id = saved.Id, IsFinding = false });
            (await dbContext.CaseNote.FirstAsync(w => w.Id == saved.Id)).IsFinding.Should().BeFalse();
        }

        [Fact]
        public async Task AnotherAnalystCannotMarkSomeoneElsesNoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var other = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            await using var dbContext = fx.GetDbContext();
            var author = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await author.InsertAsync(Draft(seed));

            var service = await ServiceAsync(dbContext, other);
            var act = () => service.SetFindingAsync(new CaseNoteFindingRequestDto { Id = saved.Id, IsFinding = true });

            await act.Should().ThrowAsync<NotFoundException>();
            (await dbContext.CaseNote.FirstAsync(w => w.Id == saved.Id)).IsFinding.Should().BeFalse();
        }

        [Fact]
        public async Task ANoteCannotBeAddedOrMarkedOnACaseLockedToSomeoneElseButCanOnYourOwnOrAFreeOneAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var other = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            await using var dbContext = fx.GetDbContext();
            var author = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var own = await author.InsertAsync(Draft(seed, "before the lock"));

            await seed.LockToAsync(other);

            await ((Func<Task>)(() => author.InsertAsync(Draft(seed, "refused"))))
                .Should().ThrowAsync<CaseLockedException>();
            await ((Func<Task>)(() => author.SetFindingAsync(new CaseNoteFindingRequestDto { Id = own.Id, IsFinding = true })))
                .Should().ThrowAsync<CaseLockedException>();

            (await dbContext.CaseNote.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(1);
            (await dbContext.CaseNote.FirstAsync(w => w.Id == own.Id)).IsFinding.Should().BeFalse();

            await seed.LockToAsync(fx.Seed.UserWithPermission);
            await author.InsertAsync(Draft(seed, "holder note"));

            await seed.LockToAsync(null);
            await author.InsertAsync(Draft(seed, "free note"));

            (await dbContext.CaseNote.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(3);
        }

        [Fact]
        public async Task AnAnalystWithoutTheActionOfANoteDoesNotSeeItInTheRepositoryListAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var withoutAction = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            var noteId = await seed.CreateNoteAsync(fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();

            var granted = await (await ServiceAsync(dbContext, fx.Seed.UserWithPermission))
                .GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue);
            var refused = await (await ServiceAsync(dbContext, withoutAction))
                .GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue);

            granted.Should().ContainSingle().Which.Id.Should().Be(noteId);
            refused.Should().BeEmpty();
        }

        [Fact]
        public async Task AUserWithRolesButNoTenantMappingSeesNoCaseThroughTheCaseRepositoryAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var name = $"{DatabaseFixture.Prefix}NoTenant{Guid.NewGuid():N}"[..40];

            var roleGuid = await dbContext.UserRegistry.Where(u => u.Name == fx.Seed.UserWithPermission)
                .Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = name, Email = $"{name}@example.invalid",
                Password = "not-used", Active = 1, PasswordLocked = 0, Deleted = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });

            try
            {
                (await new CaseRepository(dbContext, name).GetByIdActiveOnlyAsync(seed.CaseId)).Should().BeNull();
                (await new CaseRepository(dbContext, fx.Seed.UserWithPermission).GetByIdActiveOnlyAsync(seed.CaseId))
                    .Should().NotBeNull();
            }
            finally
            {
                await dbContext.UserRegistry.Where(u => u.Name == name).DeleteAsync();
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task NotesWrittenBySystemOrAskJoobyAreReadOnlyAsync(int sourceId)
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var id = await seed.CreateNoteAsync(fx.Seed.UserWithPermission, sourceId: sourceId);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.SetFindingAsync(new CaseNoteFindingRequestDto { Id = id, IsFinding = true });

            await act.Should().ThrowAsync<NotFoundException>();
            (await dbContext.CaseNote.FirstAsync(w => w.Id == id)).IsFinding.Should().BeFalse();
        }

        [Fact]
        public async Task ANoteInAnotherTenantBehavesLikeAMissingNoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var author = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await author.InsertAsync(Draft(seed));

            var foreign = await ServiceAsync(dbContext, fx.Seed.UserTenantB);
            var found = () => foreign.SetFindingAsync(new CaseNoteFindingRequestDto { Id = saved.Id, IsFinding = true });
            var missing = () => foreign.SetFindingAsync(new CaseNoteFindingRequestDto { Id = int.MaxValue, IsFinding = true });

            var first = (await found.Should().ThrowAsync<NotFoundException>()).Which.Message;
            var second = (await missing.Should().ThrowAsync<NotFoundException>()).Which.Message;
            first.Should().Be(second);
        }

        [Fact]
        public async Task MarkingAFindingNeedsThePermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var act = () => service.SetFindingAsync(new CaseNoteFindingRequestDto { Id = 1, IsFinding = true });

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task TheListReturnsNotesWithoutAnActionAndTheirNewFieldsAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();

            await dbContext.InsertAsync(new Data.Poco.CaseNote
            {
                Note = "narrative", CaseId = seed.CaseId, CaseKey = seed.CaseKey, CaseKeyValue = seed.CaseKeyValue,
                CreatedUser = fx.Seed.UserWithPermission, CreatedDate = DateTime.UtcNow, SourceId = 3, KindId = 2,
                Version = 1
            });

            var query = await CaseNoteByCaseKeyValueService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, new NullServiceChangeBus());

            var rows = await query.GetAsync(seed.CaseKey, seed.CaseKeyValue);

            var row = rows.Should().ContainSingle().Subject;
            row.SourceId.Should().Be(3);
            row.KindId.Should().Be(2);
            row.Action.Should().BeNull();
            row.Files.Should().BeEmpty();
        }


        [Fact]
        public async Task TheNoteListCarriesEachNotesFilesAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(Draft(seed));

            await dbContext.InsertAsync(new Data.Poco.CaseFile
            {
                CaseNoteId = saved.Id, CaseId = seed.CaseId, CaseKey = seed.CaseKey, CaseKeyValue = seed.CaseKeyValue,
                Name = "filing.pdf", Extension = ".pdf", ContentType = "application/pdf", Size = 10,
                Object = [1, 2, 3], CreatedUser = fx.Seed.UserWithPermission, CreatedDate = DateTime.UtcNow
            });

            var listed = (await service.GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue)).Single();

            listed.Files.Should().ContainSingle().Which.Name.Should().Be("filing.pdf");
        }

        [Fact]
        public async Task TheQueryTheCasePageReadsCarriesFilesForEveryKindAndSourceAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();

            var narrative = await seed.CreateNoteAsync(fx.Seed.UserWithPermission, kindId: 2, sourceId: 3);
            var migrated = await seed.CreateNoteAsync(fx.Seed.UserWithPermission, kindId: 4, sourceId: 2);

            foreach (var noteId in new[] { narrative, migrated })
            {
                await dbContext.InsertAsync(new Data.Poco.CaseFile
                {
                    CaseNoteId = noteId, CaseId = seed.CaseId, CaseKey = seed.CaseKey, CaseKeyValue = seed.CaseKeyValue,
                    Name = $"f{noteId}.pdf", Extension = ".pdf", ContentType = "application/pdf", Size = 5,
                    Object = [1], CreatedUser = fx.Seed.UserWithPermission, CreatedDate = DateTime.UtcNow
                });
            }

            var query = await CaseNoteByCaseKeyValueService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, new NullServiceChangeBus());

            var rows = await query.GetAsync(seed.CaseKey, seed.CaseKeyValue);

            rows.Should().HaveCount(2);
            rows.Should().OnlyContain(r => r.Files.Count == 1);
            rows.Single(r => r.Id == migrated).Files[0].Name.Should().Be($"f{migrated}.pdf");
        }

        [Fact]
        public async Task NarrativeVersionsChainAndAnUnchangedFactsHashWritesNothingAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseNoteRepository(dbContext, fx.Seed.UserWithPermission);

            Data.Poco.CaseNote Narrative(string hash) => new()
            {
                Note = "n", CaseId = seed.CaseId, CaseKey = seed.CaseKey, CaseKeyValue = seed.CaseKeyValue,
                FactsHash = hash
            };

            var first = await repository.InsertNarrativeAsync(Narrative("a"));
            var repeated = await repository.InsertNarrativeAsync(Narrative("a"));
            var second = await repository.InsertNarrativeAsync(Narrative("b"));

            first!.Version.Should().Be(1);
            first.SupersedesNoteId.Should().BeNull();
            repeated.Should().BeNull();
            second!.Version.Should().Be(2);
            second.SupersedesNoteId.Should().Be(first.Id);
            second.SourceId.Should().Be(3);
            second.KindId.Should().Be(2);
        }

        [Fact]
        public async Task ParallelNarrativesNeverShareAVersionAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);

            var writes = Enumerable.Range(0, 8).Select(async i =>
            {
                await using var dbContext = fx.GetDbContext();
                var repository = new CaseNoteRepository(dbContext, fx.Seed.UserWithPermission);

                return await repository.InsertNarrativeAsync(new Data.Poco.CaseNote
                {
                    Note = "n", CaseId = seed.CaseId, CaseKey = seed.CaseKey, CaseKeyValue = seed.CaseKeyValue,
                    FactsHash = $"hash-{i}"
                });
            });

            var saved = (await Task.WhenAll(writes)).Where(w => w is not null).ToList();

            saved.Should().HaveCount(8);
            saved.Select(w => w!.Version).Should().OnlyHaveUniqueItems();
            saved.Select(w => w!.Version).Order().Should().Equal(Enumerable.Range(1, 8));
            saved.Count(w => w!.SupersedesNoteId is null).Should().Be(1);
        }

        [Fact]
        public async Task ParallelIdenticalNarrativesWriteExactlyOneAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);

            var writes = Enumerable.Range(0, 8).Select(async _ =>
            {
                await using var dbContext = fx.GetDbContext();
                var repository = new CaseNoteRepository(dbContext, fx.Seed.UserWithPermission);

                return await repository.InsertNarrativeAsync(new Data.Poco.CaseNote
                {
                    Note = "n", CaseId = seed.CaseId, CaseKey = seed.CaseKey, CaseKeyValue = seed.CaseKeyValue,
                    FactsHash = "same"
                });
            });

            (await Task.WhenAll(writes)).Count(w => w is not null).Should().Be(1);
        }
    }
}

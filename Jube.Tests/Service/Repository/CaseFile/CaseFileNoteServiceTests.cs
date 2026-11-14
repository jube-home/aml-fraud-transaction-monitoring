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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Service.Exceptions.Repository.CaseFile;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseFile;
using Jube.Service.Repository.CaseFile.Models;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.CaseNote;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseFile
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseFileNoteServiceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static UploadedFileContent File(string name = "evidence.pdf", string content = "%PDF-1.4") =>
            new(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(content)), name, "application/pdf",
                Encoding.UTF8.GetByteCount(content));

        private static Task<CaseFileService> ServiceAsync(Jube.Data.Context.DbContext dbContext, string user) =>
            CaseFileService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus());

        [Fact]
        public async Task AFileIsAttachedToTheCallersOwnNoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var noteId = await seed.CreateNoteAsync(fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, noteId);

            saved!.CaseNoteId.Should().Be(noteId);
            (await dbContext.CaseNote.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(1);

            var listed = await service.GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue);
            listed.Should().ContainSingle().Which.CaseNoteId.Should().Be(noteId);
        }

        [Fact]
        public async Task AFileOnANoteWhoseActionTheCallerLacksIsNeitherListedNorDownloadableNorRemovableAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var withoutAction = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            var noteId = await seed.CreateNoteAsync(fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var owner = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await owner.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, noteId);
            var restricted = await ServiceAsync(dbContext, withoutAction);

            (await restricted.GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue)).Should().BeEmpty();
            await ((Func<Task>)(() => restricted.GenerateAsync(saved!.Id))).Should().ThrowAsync<NotVisibleException>();
            await ((Func<Task>)(() => restricted.RemoveAsync(saved!.Id))).Should().ThrowAsync<NotVisibleException>();

            (await owner.GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue)).Should().ContainSingle();
            (await owner.GenerateAsync(saved!.Id)).Name.Should().Be("evidence.pdf");
        }

        [Fact]
        public async Task AFileCannotBeUploadedToACaseLockedToSomeoneElseAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var other = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            await seed.LockToAsync(other);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId);

            await act.Should().ThrowAsync<CaseLockedException>();
            (await dbContext.GetTable<Data.Poco.CaseFile>().CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }

        [Fact]
        public async Task AFileCanOnlyBeRemovedByItsUploaderTheLockHolderOrASupervisorAndNeverAgainstAnotherHoldersLockAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var uploader = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            var other = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            var supervisor = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1, 18);
            await using var dbContext = fx.GetDbContext();
            var owner = await ServiceAsync(dbContext, uploader);
            var asOther = await ServiceAsync(dbContext, other);
            var asSupervisor = await ServiceAsync(dbContext, supervisor);

            var first = (await owner.UploadAsync([File("a.pdf")], seed.CaseKey, seed.CaseKeyValue, seed.CaseId))!.Id;
            var second = (await owner.UploadAsync([File("b.pdf")], seed.CaseKey, seed.CaseKeyValue, seed.CaseId))!.Id;
            var third = (await owner.UploadAsync([File("c.pdf")], seed.CaseKey, seed.CaseKeyValue, seed.CaseId))!.Id;

            await ((Func<Task>)(() => asOther.RemoveAsync(first))).Should().ThrowAsync<ForbiddenException>();

            await seed.LockToAsync(other);
            await ((Func<Task>)(() => owner.RemoveAsync(first))).Should().ThrowAsync<CaseLockedException>();
            await asOther.RemoveAsync(first);

            await seed.LockToAsync(fx.Seed.UserWithPermission);
            await ((Func<Task>)(() => asOther.RemoveAsync(second))).Should().ThrowAsync<CaseLockedException>();
            await asSupervisor.RemoveAsync(second);

            await seed.LockToAsync(null);
            await owner.RemoveAsync(third);

            (await dbContext.GetTable<Data.Poco.CaseFile>().CountAsync(w => w.CaseId == seed.CaseId && w.Deleted == 1))
                .Should().Be(3);
        }

        [Fact]
        public async Task OneNoteCarriesManyFilesAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var noteId = await seed.CreateNoteAsync(fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.UploadAsync([File("a.pdf")], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, noteId);
            await service.UploadAsync([File("b.pdf")], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, noteId);

            (await dbContext.GetTable<Data.Poco.CaseFile>().CountAsync(w => w.CaseNoteId == noteId)).Should().Be(2);
        }

        [Fact]
        public async Task ANoteIsCreatedForAFileUploadedWithoutOneAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId);

            var note = await dbContext.CaseNote.FirstAsync(w => w.Id == saved!.CaseNoteId);
            note.Note.Should().Be("Uploaded Document");
            note.SourceId.Should().Be(1);
            note.KindId.Should().Be(4);
            note.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            note.CaseId.Should().Be(seed.CaseId);
        }

        [Fact]
        public async Task AnEmptyUploadCreatesNeitherAFileNorANoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.UploadAsync(
                [new UploadedFileContent(new System.IO.MemoryStream(), "empty.txt", "text/plain", 0)],
                seed.CaseKey, seed.CaseKeyValue, seed.CaseId);

            saved.Should().BeNull();
            (await dbContext.CaseNote.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }

        [Fact]
        public async Task AFileCannotBeAttachedToSomeoneElsesNoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var other = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            var noteId = await seed.CreateNoteAsync(fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, other);

            var act = () => service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, noteId);

            await act.Should().ThrowAsync<NotVisibleException>();
            (await dbContext.GetTable<Data.Poco.CaseFile>().CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task AFileCannotBeAttachedToANoteWrittenBySystemOrAskJoobyAsync(int sourceId)
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var noteId = await seed.CreateNoteAsync(fx.Seed.UserWithPermission, sourceId: sourceId);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, noteId);

            await act.Should().ThrowAsync<NotVisibleException>();
        }

        [Fact]
        public async Task AFileCannotBeAttachedToANoteOfAnotherCaseAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var other = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var noteOnOtherCase = await other.CreateNoteAsync(fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId,
                noteOnOtherCase);

            await act.Should().ThrowAsync<NotVisibleException>();
        }

        [Fact]
        public async Task ANoteInAnotherTenantBehavesLikeAMissingNoteAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var noteId = await seed.CreateNoteAsync(fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserTenantB);

            var existing = () => service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, noteId);
            var missing = () => service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, int.MaxValue);

            var first = (await existing.Should().ThrowAsync<NotVisibleException>()).Which.Message;
            var second = (await missing.Should().ThrowAsync<NotVisibleException>()).Which.Message;
            first.Should().Be(second);
        }

        private async Task<int> CreateResearchAsync(CaseNoteSeed seed, string user, int sourceId = 1)
        {
            await using var db = fx.GetDbContext();

            return await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseResearch
            {
                CaseId = seed.CaseId, CaseKey = seed.CaseKey, CaseKeyValue = seed.CaseKeyValue, Topic = "t",
                Findings = "f", SourceId = sourceId, CreatedUser = user, CreatedDate = DateTime.UtcNow
            });
        }

        [Fact]
        public async Task AFileIsAttachedToTheCallersOwnResearchAndListedWithItsParentAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var researchId = await CreateResearchAsync(seed, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, null, researchId);

            saved!.CaseResearchId.Should().Be(researchId);
            saved.CaseNoteId.Should().BeNull();
            (await dbContext.CaseNote.CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);

            var listed = await service.GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue);
            listed.Should().ContainSingle().Which.CaseResearchId.Should().Be(researchId);
        }

        [Fact]
        public async Task TheResearchListCarriesItsFilesAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var researchId = await CreateResearchAsync(seed, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            await (await ServiceAsync(dbContext, fx.Seed.UserWithPermission))
                .UploadAsync([File("filing.pdf")], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, null, researchId);

            var research = await (await Jube.Service.Repository.CaseResearch.CaseResearchService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus()))
                .GetByCaseKeyValueAsync(seed.CaseKey, seed.CaseKeyValue);

            research.Should().ContainSingle().Which.Files.Should().ContainSingle().Which.Name.Should().Be("filing.pdf");
        }

        [Fact]
        public async Task AFileCannotBeAttachedToSomeoneElsesAskJoobyOrAnotherCasesOrAnotherTenantsResearchAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            await using var other = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var someoneElse = await seed.CreateUserAsync(fx.Seed.UserWithPermission, 1);
            var theirs = await CreateResearchAsync(seed, fx.Seed.UserWithPermission);
            var byAgent = await CreateResearchAsync(seed, fx.Seed.UserWithPermission, 2);
            var onOtherCase = await CreateResearchAsync(other, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();

            var asOther = await ServiceAsync(dbContext, someoneElse);
            var asOwner = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var foreign = await ServiceAsync(dbContext, fx.Seed.UserTenantB);

            var messages = new[]
            {
                (await ((Func<Task>)(() => asOther.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, null, theirs)))
                    .Should().ThrowAsync<NotVisibleException>()).Which.Message,
                (await ((Func<Task>)(() => asOwner.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, null, byAgent)))
                    .Should().ThrowAsync<NotVisibleException>()).Which.Message,
                (await ((Func<Task>)(() => asOwner.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, null, onOtherCase)))
                    .Should().ThrowAsync<NotVisibleException>()).Which.Message,
                (await ((Func<Task>)(() => foreign.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, null, theirs)))
                    .Should().ThrowAsync<NotVisibleException>()).Which.Message,
                (await ((Func<Task>)(() => asOwner.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, null, int.MaxValue)))
                    .Should().ThrowAsync<NotVisibleException>()).Which.Message
            };

            messages.Distinct().Should().HaveCount(1);
            (await dbContext.GetTable<Data.Poco.CaseFile>().CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }

        [Fact]
        public async Task ANoteAndResearchTogetherAreRefusedAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var noteId = await seed.CreateNoteAsync(fx.Seed.UserWithPermission);
            var researchId = await CreateResearchAsync(seed, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.UploadAsync([File()], seed.CaseKey, seed.CaseKeyValue, seed.CaseId, noteId, researchId);

            await act.Should().ThrowAsync<DtoValidationException>();
            (await dbContext.GetTable<Data.Poco.CaseFile>().CountAsync(w => w.CaseId == seed.CaseId)).Should().Be(0);
        }

        [Fact]
        public async Task TheDatabaseRefusesAFileWithBothParentsOrNoneAsync()
        {
            await using var seed = await CaseNoteSeed.CreateAsync(fx, fx.Seed.UserWithPermission);
            var noteId = await seed.CreateNoteAsync(fx.Seed.UserWithPermission);
            var researchId = await CreateResearchAsync(seed, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();

            Data.Poco.CaseFile Make(int? note, int? research) => new()
            {
                CaseNoteId = note, CaseResearchId = research, CaseId = seed.CaseId, CaseKey = seed.CaseKey,
                CaseKeyValue = seed.CaseKeyValue, Name = "x.txt", Extension = ".txt", ContentType = "text/plain",
                Size = 1, Object = [1], CreatedUser = fx.Seed.UserWithPermission, CreatedDate = DateTime.UtcNow
            };

            var both = () => dbContext.InsertWithInt32IdentityAsync(Make(noteId, researchId));
            var neither = () => dbContext.InsertWithInt32IdentityAsync(Make(null, null));

            (await both.Should().ThrowAsync<Npgsql.PostgresException>()).Which.ConstraintName
                .Should().Be("CK_CaseFile_ExactlyOneParent");

            (await neither.Should().ThrowAsync<Npgsql.PostgresException>()).Which.ConstraintName
                .Should().Be("CK_CaseFile_ExactlyOneParent");
        }
    }
}

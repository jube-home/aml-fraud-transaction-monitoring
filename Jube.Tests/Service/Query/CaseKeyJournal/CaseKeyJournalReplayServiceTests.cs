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
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Query.CaseKeyJournal;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseKeyJournalReplayServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty;

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = connectionString,
                ["ReportConnectionString"] = connectionString
            });

        private readonly List<Guid> createdArchiveGuids = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdWorkflowIds = [];
        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdXPathIds = [];

        private string Me => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseKeyJournalReplay.Where(w => createdCaseIds.Contains(w.CaseId)).DeleteAsync();

            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();

            await dbContext.Archive.Where(w => createdArchiveGuids.Contains(w.EntityAnalysisModelInstanceEntryGuid))
                .DeleteAsync();

            var xPathGuids = dbContext.CaseWorkflowXPath.Where(s => createdXPathIds.Contains(s.Id)).Select(s => s.Guid);

            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathRole>()
                .Where(w => xPathGuids.Contains(w.CaseWorkflowXPathGuid)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowXPathVersion>()
                .Where(w => w.CaseWorkflowXPathId != null && createdXPathIds.Contains(w.CaseWorkflowXPathId.Value))
                .DeleteAsync();

            await dbContext.CaseWorkflowXPath.Where(w => createdXPathIds.Contains(w.Id)).DeleteAsync();

            await dbContext.EntityAnalysisModelRequestXpath
                .Where(w => w.EntityAnalysisModelId != null && createdModelIds.Contains(w.EntityAnalysisModelId.Value))
                .DeleteAsync();

            var caseWorkflowGuids =
                dbContext.CaseWorkflow.Where(s => createdWorkflowIds.Contains(s.Id)).Select(s => s.Guid);

            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();

            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();

            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseKeyJournalReplayService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return CaseKeyJournalReplayService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext)
        {
            var saved = await new EntityAnalysisModelRepository(dbContext, Me).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                }).ConfigureAwait(false);
            createdModelIds.Add(saved.Id);

            return saved.Id;
        }

        private async Task<Guid> CreateWorkflowAsync(DbContext dbContext, int modelId, bool grantRoleToUser = true)
        {
            var guid = Guid.NewGuid();

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = guid,
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);
            createdWorkflowIds.Add(id);

            if (!grantRoleToUser)
            {
                return guid;
            }

            var roleGuid = await dbContext.UserRegistry.Where(u => u.Name == Me)
                .Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = guid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            return guid;
        }

        private async Task CreateLocationAsync(DbContext dbContext, int model, Guid workflow)
        {
            foreach (var (name, type, path, column) in new[]
                     {
                         ("Lat", 6, "$.payload.Lat", "Latitude"), ("Lon", 7, "$.payload.Lon", "Longitude")
                     })
            {
                await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = model, Name = name, DataTypeId = type, XPath = $"$.{name}", Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedUser = Me, CreatedDate = DateTime.UtcNow
                });

                var guid = Guid.NewGuid();
                var workflowId = await dbContext.CaseWorkflow.Where(w => w.Guid == workflow).Select(w => w.Id)
                    .FirstAsync();

                createdXPathIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowXPath
                {
                    Guid = guid, CaseWorkflowId = workflowId, Name = column, XPath = path, Active = 1, Locked = 0,
                    Deleted = 0, Drill = 0, Version = 1, CreatedUser = Me, CreatedDate = DateTime.UtcNow
                }));

                var roleGuid = await dbContext.UserRegistry.Where(u => u.Name == Me)
                    .Select(u => u.RoleRegistryGuid).FirstAsync();

                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowXPathRole
                {
                    CaseWorkflowXPathGuid = guid, Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid,
                    CreatedUser = Me, CreatedDate = DateTime.UtcNow, Deleted = 0
                });
            }
        }

        private async Task CreateArchiveAsync(DbContext dbContext, int modelId, string accountId, double latitude,
            double longitude, DateTime at)
        {
            var guid = Guid.NewGuid();
            var json = new JObject
            {
                ["payload"] = new JObject { ["AccountId"] = accountId, ["Lat"] = latitude, ["Lon"] = longitude },
                ["tag"] = new JArray()
            };

            await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json.ToString(Newtonsoft.Json.Formatting.None),
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = modelId,
                EntryKeyValue = accountId,
                ResponseElevation = 5,
                ActivationRuleCount = 0,
                CreatedDate = at,
                ReferenceDate = at,
            }).ConfigureAwait(false);
            createdArchiveGuids.Add(guid);
        }

        private async Task<int> CreateCaseAsync(DbContext dbContext, Guid workflow, string? lockedUser = null,
            string? caseKeyValue = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflow,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Locked = (byte)(lockedUser is null ? 0 : 1),
                LockedUser = lockedUser,
                ClosedStatusId = 0,
                CaseKey = "AccountId",
                CaseKeyValue = caseKeyValue,
            });
            createdCaseIds.Add(id);

            return id;
        }

        private async Task<(Guid Workflow, int Model, int Case)> SetupAsync(DbContext dbContext,
            string? lockedUser = null, string? caseKeyValue = null)
        {
            var model = await CreateModelAsync(dbContext);
            var workflow = await CreateWorkflowAsync(dbContext, model);

            return (workflow, model, await CreateCaseAsync(dbContext, workflow, lockedUser, caseKeyValue));
        }

        private static CaseKeyJournalReplayRequestDto Request(int caseId, string name = "Two countries",
            int days = 30, string? merge = null, string? filter = null)
        {
            return new CaseKeyJournalReplayRequestDto
            {
                CaseId = caseId, Name = name, Days = days, Merge = merge, Filter = filter
            };
        }

        [Fact]
        public async Task AReplayIsOnlySettingsSharedOnTheCaseAndListedNewestFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model, caseId) = await SetupAsync(dbContext);
            var (_, _, other) = await SetupAsync(dbContext);
            await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = model, Name = "Channel", DataTypeId = 1, XPath = "$.Channel", Active = 1,
                Locked = 0, Deleted = 0, Version = 1, CreatedUser = Me, CreatedDate = DateTime.UtcNow
            });
            var service = await BuildServiceAsync(dbContext, Me);
            var filter = """{"condition":"AND","rules":[{"id":"Payload.Channel","operator":"equal","value":"CP"}]}""";

            var first = await service.SaveAsync(Request(caseId, "  First  ", 7));
            var second = await service.SaveAsync(Request(caseId, "Second", 90,
                "[{\"key\":\"DeviceId\",\"value\":\"D-9\"}]", filter));

            first.Name.Should().Be("First");
            first.Days.Should().Be(7);
            first.Merge.Should().BeNull();
            first.Filter.Should().BeNull();
            first.CreatedUser.Should().Be(Me);
            second.Merge.Should().Contain("D-9");
            second.Filter.Should().Be(filter);

            (await service.ListAsync(caseId)).Replays.Select(replay => replay.Guid).Should()
                .Equal(second.Guid, first.Guid);
            (await service.ListAsync(other)).Replays.Should().BeEmpty();
        }

        [Fact]
        public async Task ADeletedReplayIsNoLongerOfferedAndTheRowIsKeptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, caseId) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);
            var saved = await service.SaveAsync(Request(caseId));

            await service.DeleteAsync(saved.Guid);

            (await service.ListAsync(caseId)).Replays.Should().BeEmpty();

            var row = await dbContext.CaseKeyJournalReplay.FirstAsync(w => w.Guid == saved.Guid);

            row.Deleted.Should().Be(1);
            row.DeletedUser.Should().Be(Me);
            row.DeletedDate.Should().NotBeNull();
            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Guid));
        }

        [Fact]
        public async Task TheReplaySettingsAreValidatedAndACaseHoldsNoMoreThanTenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, caseId) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            async Task<string[]> CodesAsync(CaseKeyJournalReplayRequestDto request)
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.SaveAsync(request));

                return ex.Result.Errors.Select(e => e.ErrorCode).ToArray();
            }

            (await CodesAsync(Request(caseId, " "))).Should().Contain("ReplayNameInvalid");
            (await CodesAsync(Request(caseId, new string('x', 101)))).Should().Contain("ReplayNameInvalid");
            (await CodesAsync(Request(caseId, "x", 31))).Should().Contain("ReplayWindowInvalid");

            var lens = Request(caseId, "x");
            lens.Lens = "everything";

            (await CodesAsync(lens)).Should().Contain("LensInvalid");
            (await CodesAsync(Request(caseId, "x", 30, "nonsense"))).Should().Contain("MergeInvalid");
            (await CodesAsync(Request(caseId, "x", 30, null, "not json"))).Should().Contain("FilterInvalid");

            for (var i = 0; i < CaseKeyJournalReplayService.MaximumReplays; i++)
            {
                await service.SaveAsync(Request(caseId, $"Replay {i}"));
            }

            (await CodesAsync(Request(caseId, "One too many"))).Should().Contain("ReplayLimitInvalid");

            var first = (await service.ListAsync(caseId)).Replays[0];
            await service.DeleteAsync(first.Guid);

            (await service.SaveAsync(Request(caseId, "Room again"))).Name.Should().Be("Room again");
        }

        [Fact]
        public async Task ACaseLockedToAnotherUserCannotHaveReplaysSavedOrDeletedButMyOwnLockCanAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, theirCase) = await SetupAsync(dbContext, fx.Seed.UserTenantB);
            var (_, _, ownCase) = await SetupAsync(dbContext, Me);
            var service = await BuildServiceAsync(dbContext, Me);

            await Assert.ThrowsAsync<ConflictException>(() => service.SaveAsync(Request(theirCase)));

            var saved = await service.SaveAsync(Request(ownCase));
            await dbContext.Case.Where(c => c.Id == ownCase).Set(c => c.LockedUser, fx.Seed.UserTenantB)
                .UpdateAsync();

            await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(saved.Guid));
            (await service.ListAsync(ownCase)).Replays.Should().HaveCount(1);
        }

        [Fact]
        public async Task TheSummaryWorksTheSavedSettingsOutAgainFromTheArchiveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = $"{DatabaseFixture.Prefix}A{Guid.NewGuid():N}"[..30];
            var (workflow, model, caseId) = await SetupAsync(dbContext, caseKeyValue: key);
            await CreateLocationAsync(dbContext, model, workflow);
            var now = DateTime.UtcNow;
            await CreateArchiveAsync(dbContext, model, key, 48.8566, 2.3522, now.AddDays(-3));
            await CreateArchiveAsync(dbContext, model, key, -33.8688, 151.2093, now.AddDays(-3).AddHours(2));
            await CreateArchiveAsync(dbContext, model, key, 51.5074, -0.1278, now.AddDays(-60));
            await CreateArchiveAsync(dbContext, model, "someone-else", 0.5, 0.5, now.AddDays(-3));
            var service = await BuildServiceAsync(dbContext, Me);
            var recent = await service.SaveAsync(Request(caseId, "Recent", 7));
            var long_ = await service.SaveAsync(Request(caseId, "Long", 90));

            var week = await service.SummaryAsync(recent.Guid);

            week.Points.Should().BeEmpty();
            week.Meta.Matched.Should().Be(2);
            week.Meta.Located.Should().Be(2);
            week.Summary.Impossible.Should().Be(1);
            week.Summary.Worst.Single().Kilometres.Should().BeGreaterThan(16000);

            var quarter = await service.SummaryAsync(long_.Guid);

            quarter.Meta.Matched.Should().Be(3);
            quarter.Summary.Legs.Should().Be(2);

            await dbContext.CaseKeyJournalReplay.Where(w => w.Guid == recent.Guid)
                .Set(w => w.FilterJson, """{"condition":"AND","rules":[{"id":"Payload.Lat","operator":"greater","value":"0"}]}""")
                .UpdateAsync();

            (await service.SummaryAsync(recent.Guid)).Meta.Matched.Should().Be(1);
            await Assert.ThrowsAsync<NotFoundException>(() => service.SummaryAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task TheSpeedAndDistanceLimitsAreSavedWithTheReplayAndUsedWhenItIsSummarisedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = $"{DatabaseFixture.Prefix}A{Guid.NewGuid():N}"[..30];
            var (workflow, model, caseId) = await SetupAsync(dbContext, caseKeyValue: key);
            await CreateLocationAsync(dbContext, model, workflow);
            var at = DateTime.UtcNow.AddDays(-2);
            await CreateArchiveAsync(dbContext, model, key, 51.5074, -0.1278, at);
            await CreateArchiveAsync(dbContext, model, key, 48.8566, 2.3522, at.AddHours(1));
            var service = await BuildServiceAsync(dbContext, Me);

            var plain = await service.SaveAsync(Request(caseId, "Default"));
            var strict = await service.SaveAsync(new CaseKeyJournalReplayRequestDto
            {
                CaseId = caseId, Name = "Strict", Days = 30, LimitKmh = 300, MinimumKilometres = 100,
                ImpossibleOnly = true
            });

            plain.ImpossibleOnly.Should().BeFalse();
            strict.ImpossibleOnly.Should().BeTrue();
            plain.LimitKmh.Should().BeNull();
            plain.MinimumKilometres.Should().BeNull();
            strict.LimitKmh.Should().Be(300);
            strict.MinimumKilometres.Should().Be(100);

            (await service.SummaryAsync(plain.Guid)).Summary.Impossible.Should().Be(0);

            var summary = await service.SummaryAsync(strict.Guid);

            summary.Summary.Impossible.Should().Be(1);
            summary.Summary.LimitKmh.Should().Be(300);
            summary.Summary.MinimumKilometres.Should().Be(100);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.SaveAsync(
                new CaseKeyJournalReplayRequestDto { CaseId = caseId, Name = "Bad", Days = 30, LimitKmh = 0 }));

            ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain("TravelLimitInvalid");
        }

        [Fact]
        public async Task TheLensAndShowingEverythingAreSavedWithTheViewAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, caseId) = await SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, Me);

            var plain = await service.SaveAsync(Request(caseId, "Plain"));
            var map = await service.SaveAsync(new CaseKeyJournalReplayRequestDto
            {
                CaseId = caseId, Name = "Map", Days = 7, ShowAll = true, Lens = "Rules"
            });
            var grid = await service.SaveAsync(new CaseKeyJournalReplayRequestDto
            {
                CaseId = caseId, Name = "Grid", Days = 7, Lens = "payload"
            });

            plain.ShowAll.Should().BeFalse();
            plain.Lens.Should().BeNull();
            map.ShowAll.Should().BeTrue();
            map.Lens.Should().Be("rules");
            grid.Lens.Should().BeNull();

            var listed = (await service.ListAsync(caseId)).Replays.Single(replay => replay.Guid == map.Guid);

            listed.Lens.Should().Be("rules");
            listed.ShowAll.Should().BeTrue();
        }

        [Fact]
        public async Task ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, caseId) = await SetupAsync(dbContext);
            var hiddenModel = await CreateModelAsync(dbContext);
            var hiddenWorkflow = await CreateWorkflowAsync(dbContext, hiddenModel, grantRoleToUser: false);
            var hiddenCase = await CreateCaseAsync(dbContext, hiddenWorkflow);
            var denied = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var allowed = await BuildServiceAsync(dbContext, Me);
            var saved = await allowed.SaveAsync(Request(caseId));

            await Assert.ThrowsAsync<ForbiddenException>(() => denied.SaveAsync(Request(caseId)));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.ListAsync(caseId));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.DeleteAsync(saved.Guid));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.SummaryAsync(saved.Guid));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.ListAsync(hiddenCase));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.SaveAsync(Request(hiddenCase)));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.ListAsync(int.MaxValue));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.SaveAsync(null));
        }
    }
}

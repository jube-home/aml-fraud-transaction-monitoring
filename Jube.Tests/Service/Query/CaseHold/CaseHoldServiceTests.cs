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
using Jube.Data.Repository.Models.Hold;
using Jube.Dto.Hold;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseHold;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.CaseHold;
using Jube.Service.Query.CaseHold;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.CaseHold.Models;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Data.Repository;

namespace Jube.Test.Service.Query.CaseHold
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseHoldServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Secret = "JUBE_HOOK_CASEHOLDTEST";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> caseIds = [];
        private readonly List<Guid> entryGuids = [];
        private readonly List<int> hookIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> statusIds = [];
        private readonly List<int> workflowIds = [];

        public Task InitializeAsync()
        {
            Environment.SetEnvironmentVariable(Secret, "key-1");

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            Environment.SetEnvironmentVariable(Secret, null);

            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseWorkflowDispatch>()
                .Where(w => w.CaseId != null && caseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.Case.Where(w => caseIds.Contains(w.Id)).DeleteAsync();

            var archiveIds = await dbContext.Archive.Where(a => entryGuids.Contains(a.EntityAnalysisModelInstanceEntryGuid))
                .Select(a => a.Id).ToListAsync();
            await dbContext.ArchiveVersion.Where(v => archiveIds.Contains(v.ArchiveId)).DeleteAsync();
            await dbContext.Archive.Where(a => entryGuids.Contains(a.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();

            await dbContext.CaseWorkflowReleaseHookVersion
                .Where(w => w.CaseWorkflowReleaseHookId != null && hookIds.Contains(w.CaseWorkflowReleaseHookId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflowReleaseHook.Where(w => hookIds.Contains(w.Id)).DeleteAsync();

            var statusGuids = dbContext.CaseWorkflowStatus.Where(s => statusIds.Contains(s.Id)).Select(s => s.Guid);
            var workflowGuids = dbContext.CaseWorkflow.Where(s => workflowIds.Contains(s.Id)).Select(s => s.Guid);

            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => statusGuids.Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => workflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => statusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseHoldService> BuildServiceAsync(DbContext dbContext, string? userName,
            ILog? log = null)
        {
            return CaseHoldService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                new NullServiceChangeBus(), LoopbackAllowed.Environment, TestLog.NoOp);
        }

        private static Task<Guid> RoleGuidAsync(DbContext dbContext, string userName) =>
            dbContext.UserRegistry.Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

        private static ResponseHold Held(ResponseHoldState state = ResponseHoldState.Held)
        {
            return new ResponseHold
            {
                StateId = (int)state, RangeId = 3, RangeName = "Review hold", RangeMinimum = 10, RangeMaximum = 19,
                Elevation = 12, StampedDate = DateTime.UtcNow.AddMinutes(-5),
                Legs = [new ResponseHoldLeg { KindId = (int)ResponseHoldLegKind.Webhook, OutcomeId = 1, Date = DateTime.UtcNow }]
            };
        }

        private async Task<SeededHold> SeedAsync(DbContext dbContext, string ownerUser,
            ResponseHold? hold = null, string paymentId = "P-1", string sourceSystem = "Faster",
            string? caseKeyValue = null, string? lockedUser = null, bool diary = false)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, ownerUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                    Locked = 0, Deleted = 0
                });
            modelIds.Add(model.Id);

            var workflowGuid = Guid.NewGuid();

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40], Guid = workflowGuid,
                EntityAnalysisModelId = model.Id, EnableVisualisation = 1, VisualisationRegistryGuid = Guid.NewGuid(),
                Active = 1, Locked = 0, Deleted = 0, CreatedUser = ownerUser, CreatedDate = DateTime.UtcNow
            });
            workflowIds.Add(workflowId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = workflowGuid, Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleGuidAsync(dbContext, ownerUser), CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow, Deleted = 0
            });

            var statusGuid = Guid.NewGuid();

            var statusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40], Guid = statusGuid,
                CaseWorkflowId = workflowId, ForeColor = "#112233", BackColor = "#445566", Active = 1, Locked = 0,
                Deleted = 0, Priority = 1, CreatedUser = ownerUser, CreatedDate = DateTime.UtcNow
            });
            statusIds.Add(statusId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = statusGuid, Guid = Guid.NewGuid(),
                RoleRegistryGuid = await RoleGuidAsync(dbContext, ownerUser), CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow, Deleted = 0
            });

            var key = caseKeyValue ?? $"{DatabaseFixture.Prefix}Ckv{Guid.NewGuid():N}";
            var entryGuid = Guid.NewGuid();
            var json = new JObject
            {
                ["payload"] = new JObject { ["AccountId"] = key, ["PaymentId"] = paymentId, ["SourceSystem"] = sourceSystem }
            }.ToString(Formatting.None);

            if (hold != null)
            {
                json = ResponseHoldJson.Write(json, hold);
            }

            entryGuids.Add(entryGuid);

            await dbContext.InsertAsync(new Data.Poco.Archive
            {
                Json = json, EntityAnalysisModelInstanceEntryGuid = entryGuid, EntryKeyValue = paymentId,
                EntityAnalysisModelId = model.Id, ResponseElevation = 12, ActivationRuleCount = 1,
                CreatedDate = DateTime.UtcNow, ReferenceDate = DateTime.UtcNow, Version = 1
            });

            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = entryGuid, CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid, CreatedDate = DateTime.UtcNow,
                Locked = (byte)(lockedUser == null ? 0 : 1), LockedUser = lockedUser, ClosedStatusId = 0,
                CaseKey = "AccountId", CaseKeyValue = key,
                Diary = (byte)(diary ? 1 : 0), DiaryDate = diary ? DateTime.UtcNow.AddHours(2) : null,
                DiaryTypeId = (byte)CaseDiaryType.PaymentHold, Rating = 3, LastClosedStatus = 0, Json = json
            });
            caseIds.Add(caseId);

            return new SeededHold(caseId, entryGuid, workflowGuid, key);
        }

        private async Task HookAsync(DbContext dbContext, Guid workflowGuid, string name, string releaseUrl,
            string? refuseUrl = null, string? sourceField = null, string? sourceValue = null,
            string? secretVariable = Secret, byte method = 1, string? body = null, bool active = true)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowReleaseHook
            {
                Guid = Guid.NewGuid(), CaseWorkflowGuid = workflowGuid, Name = name, Active = (byte)(active ? 1 : 0),
                Locked = 0, SourceField = sourceField, SourceValue = sourceValue, ReleaseUrl = releaseUrl,
                RefuseUrl = refuseUrl, HttpEndpointTypeId = method,
                HeaderName = secretVariable == null ? null : "X-Hook-Key", CredentialVariable = secretVariable, Body = body,
                Deleted = 0, Version = 1, CreatedUser = "test", CreatedDate = DateTime.UtcNow
            });
            hookIds.Add(id);
        }

        private static CaseHoldDecisionDto Decision(SeededHold seeded, string? reason = "Verified with the customer") =>
            new() { CaseId = seeded.CaseId, EntityAnalysisModelInstanceEntryGuid = seeded.EntryGuid, Reason = reason };

        private async Task<Data.Poco.Archive> ArchiveAsync(Guid guid)
        {
            await using var dbContext = fx.GetDbContext();

            return await dbContext.Archive.SingleAsync(a => a.EntityAnalysisModelInstanceEntryGuid == guid);
        }

        private async Task<List<Data.Poco.CaseWorkflowDispatch>> DispatchesAsync(int caseId)
        {
            await using var dbContext = fx.GetDbContext();

            return await dbContext.GetTable<Data.Poco.CaseWorkflowDispatch>().Where(w => w.CaseId == caseId)
                .ToListAsync();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, userName));

            ex.Code.Should().Be("NotAuthenticated");
        }

        [Fact]
        public async Task CreateWithAUserWithoutATenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task ListGivesTheHoldsOfTheCaseWithTheRangeTheDeadlineAndTheMessagesAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held(), diary: true);
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var holds = await service.ListAsync(seeded.CaseId);

            var hold = holds.Should().ContainSingle().Subject;
            hold.EntityAnalysisModelInstanceEntryGuid.Should().Be(seeded.EntryGuid);
            hold.StateId.Should().Be((int)ResponseHoldState.Held);
            hold.RangeName.Should().Be("Review hold");
            hold.Elevation.Should().Be(12);
            hold.DeadlineDate.Should().NotBeNull();
            hold.ReleaseHookName.Should().Be("Faster payments");
            hold.RefuseCallsHook.Should().BeFalse();
            hold.Legs.Should().ContainSingle();
        }

        [Fact]
        public async Task ListIncludesOtherTransactionsWithTheSameKeyAndLeavesOutOtherKeysAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var first = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());

            var sameKey = Guid.NewGuid();
            entryGuids.Add(sameKey);
            var model = await dbContext.CaseWorkflow.Where(w => w.Guid == first.WorkflowGuid)
                .Select(w => w.EntityAnalysisModelId).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.Archive
            {
                Json = ResponseHoldJson.Write(new JObject
                {
                    ["payload"] = new JObject { ["AccountId"] = first.CaseKeyValue, ["PaymentId"] = "P-2" }
                }.ToString(Formatting.None), Held(ResponseHoldState.Declined)),
                EntityAnalysisModelInstanceEntryGuid = sameKey, EntityAnalysisModelId = model, EntryKeyValue = "P-2",
                ResponseElevation = 40, CreatedDate = DateTime.UtcNow, ReferenceDate = DateTime.UtcNow.AddMinutes(-1),
                Version = 1
            });

            var other = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var holds = await service.ListAsync(first.CaseId);

            holds.Select(s => s.EntityAnalysisModelInstanceEntryGuid).Should().BeEquivalentTo([first.EntryGuid, sameKey]);
            holds.Single(s => s.EntityAnalysisModelInstanceEntryGuid == sameKey).StateId
                .Should().Be((int)ResponseHoldState.Declined);
            holds.Should().NotContain(s => s.EntityAnalysisModelInstanceEntryGuid == other.EntryGuid);
        }

        [Fact]
        public async Task ListWithoutThePermissionOrForACaseOfAnotherTenantIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());

            var without = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            await Assert.ThrowsAsync<ForbiddenException>(() => without.ListAsync(seeded.CaseId));

            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.ListAsync(seeded.CaseId));
        }

        [Fact]
        public async Task ReleaseCallsTheHookWithTheCredentialAndOnA200RecordsReleasedWithItsDecisionAndVersionAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held(), "P/1 ?x");
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments",
                receiver.BaseUrl + "/release/[@Payload.PaymentId@]?level=[@Hold.Elevation@]");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.ReleaseAsync(Decision(seeded));

            result.StateId.Should().Be((int)ResponseHoldState.Released);
            result.HttpStatus.Should().Be(200);
            result.ReleaseHookName.Should().Be("Faster payments");

            var call = receiver.Calls().Should().ContainSingle().Subject;
            call.Method.Should().Be("POST");
            call.PathAndQuery.Should().Be("/release/P%2F1%20%3Fx?level=12");
            call.Header.Should().Be("key-1");

            var body = JObject.Parse(call.Body);
            body["action"]!.Value<string>().Should().Be("release");
            body["reason"]!.Value<string>().Should().Be("Verified with the customer");
            body["user"]!.Value<string>().Should().Be(fx.Seed.UserWithPermission);
            body["entityAnalysisModelInstanceEntryGuid"]!.Value<string>().Should().Be(seeded.EntryGuid.ToString());

            var archive = await ArchiveAsync(seeded.EntryGuid);
            archive.Version.Should().Be(2);
            var hold = ResponseHoldJson.Read(archive.Json)!;
            hold.StateId.Should().Be((int)ResponseHoldState.Released);
            hold.Decision!.Actor.Should().Be(fx.Seed.UserWithPermission);
            hold.Decision.Reason.Should().Be("Verified with the customer");
            hold.Decision.ReleaseHookName.Should().Be("Faster payments");
            hold.Decision.HttpStatus.Should().Be(200);
            hold.Legs.Should().Contain(l => l.KindId == (int)ResponseHoldLegKind.ReleaseHook
                                            && l.OutcomeId == (int)ResponseHoldLegOutcome.Sent && l.HttpStatus == 200);
            archive.Json.Should().NotContain("key-1");

            await using var read = fx.GetDbContext();
            var versions = await read.ArchiveVersion.Where(v => v.EntityAnalysisModelInstanceEntryGuid == seeded.EntryGuid)
                .ToListAsync();
            ResponseHoldJson.Read(versions.Single().Json)!.StateId.Should().Be((int)ResponseHoldState.Held);

            var dispatch = (await DispatchesAsync(seeded.CaseId)).Should().ContainSingle().Subject;
            dispatch.SourceKindId.Should().Be((byte)CaseDispatchSourceKind.ReleaseHook);
            dispatch.OutcomeId.Should().Be((byte)CaseDispatchOutcome.Sent);
            dispatch.ResponseStatus.Should().Be(200);
            dispatch.SourceName.Should().Be("Faster payments");
            (dispatch.Destination ?? string.Empty).Should().NotContain("key-1");

            var caseJson = await read.Case.Where(c => c.Id == seeded.CaseId).Select(c => c.Json).FirstAsync();
            ResponseHoldJson.Read(caseJson)!.StateId.Should().Be((int)ResponseHoldState.Released);
        }

        [Theory]
        [InlineData(500)]
        [InlineData(404)]
        [InlineData(202)]
        public async Task AnAnswerOtherThanExactly200LeavesThePaymentHeldAndRecordsTheFailedAttemptAsync(int status)
        {
            using var receiver = new Receiver(status);
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.ReleaseAsync(Decision(seeded));

            var ex = await act.Should().ThrowAsync<HoldHookException>();
            ex.Which.Code.Should().Be("HoldHookFailed");

            var archive = await ArchiveAsync(seeded.EntryGuid);
            var hold = ResponseHoldJson.Read(archive.Json)!;
            hold.StateId.Should().Be((int)ResponseHoldState.Held);
            hold.Decision.Should().BeNull();
            hold.Legs.Should().Contain(l => l.KindId == (int)ResponseHoldLegKind.ReleaseHook
                                            && l.OutcomeId == (int)ResponseHoldLegOutcome.Failed);
            archive.Version.Should().Be(2, "the failed attempt is itself a version");

            var dispatch = (await DispatchesAsync(seeded.CaseId)).Should().ContainSingle().Subject;
            dispatch.OutcomeId.Should().Be((byte)CaseDispatchOutcome.Failed);
        }

        [Fact]
        public async Task ASecondReleaseOfTheSamePaymentIsRefusedAndTheHookIsNotCalledAgainAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.ReleaseAsync(Decision(seeded));

            await Assert.ThrowsAsync<HoldStateException>(() => service.ReleaseAsync(Decision(seeded)));
            await Assert.ThrowsAsync<HoldStateException>(() => service.RefuseAsync(Decision(seeded)));

            receiver.Calls().Should().ContainSingle();
        }

        [Fact]
        public async Task TwoReleasesAtOnceCallTheHookExactlyOnceAsync()
        {
            using var receiver = new Receiver();
            await using var setup = fx.GetDbContext();
            var seeded = await SeedAsync(setup, fx.Seed.UserWithPermission, Held());
            await HookAsync(setup, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            async Task<bool> TryAsync()
            {
                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

                try
                {
                    await service.ReleaseAsync(Decision(seeded));

                    return true;
                }
                catch (HoldStateException)
                {
                    return false;
                }
            }

            var results = await Task.WhenAll(Task.Run(TryAsync), Task.Run(TryAsync));

            results.Count(r => r).Should().Be(1);
            receiver.Calls().Should().ContainSingle();
        }

        [Fact]
        public async Task ReleaseWithoutThePermissionIsRefusedAndNothingIsCalledAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.ReleaseAsync(Decision(seeded)));
            ex.RequiredSpecifications.Should().Equal(68);

            receiver.Calls().Should().BeEmpty();
            (await ArchiveAsync(seeded.EntryGuid)).Version.Should().Be(1);
        }

        [Fact]
        public async Task ReleaseWithNoHandlingHookIsRefusedAndNothingChangesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NoReleaseHookException>(() => service.ReleaseAsync(Decision(seeded)));

            var archive = await ArchiveAsync(seeded.EntryGuid);
            archive.Version.Should().Be(1);
            ResponseHoldJson.Read(archive.Json)!.StateId.Should().Be((int)ResponseHoldState.Held);
        }

        [Fact]
        public async Task TheHookWhoseSourceFieldMatchesThePayloadHandlesItAndAnInactiveHookDoesNotAsync()
        {
            using var generic = new Receiver();
            using var faster = new Receiver();
            using var inactive = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held(), sourceSystem: "Faster");
            await HookAsync(dbContext, seeded.WorkflowGuid, "Generic", generic.BaseUrl + "/release");
            await HookAsync(dbContext, seeded.WorkflowGuid, "Bacs", generic.BaseUrl + "/bacs", sourceField: "SourceSystem",
                sourceValue: "Bacs");
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster", faster.BaseUrl + "/release",
                sourceField: "SourceSystem", sourceValue: "FASTER");
            await HookAsync(dbContext, seeded.WorkflowGuid, "Off", inactive.BaseUrl + "/release",
                sourceField: "SourceSystem", sourceValue: "Faster", active: false);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            (await service.ListAsync(seeded.CaseId)).Single().ReleaseHookName.Should().Be("Faster");

            await service.ReleaseAsync(Decision(seeded));

            faster.Calls().Should().ContainSingle();
            generic.Calls().Should().BeEmpty();
            inactive.Calls().Should().BeEmpty();
        }

        [Fact]
        public async Task AHookWhoseAddressIsNotLiteralIsRefusedAndRecordedWithoutCallingAnythingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Bad", "https://[@Payload.PaymentId@]/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<HoldHookException>(() => service.ReleaseAsync(Decision(seeded)));

            var hold = ResponseHoldJson.Read((await ArchiveAsync(seeded.EntryGuid)).Json)!;
            hold.StateId.Should().Be((int)ResponseHoldState.Held);
            hold.Legs.Should().Contain(l => l.OutcomeId == (int)ResponseHoldLegOutcome.Refused);

            (await DispatchesAsync(seeded.CaseId)).Single().OutcomeId.Should().Be((byte)CaseDispatchOutcome.Refused);
        }

        [Fact]
        public async Task AHookWhoseCredentialVariableIsNotSetIsNotCalledAsync()
        {
            using var receiver = new Receiver();
            Environment.SetEnvironmentVariable(Secret, null);
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<HoldHookException>(() => service.ReleaseAsync(Decision(seeded)));

            receiver.Calls().Should().BeEmpty();
            ResponseHoldJson.Read((await ArchiveAsync(seeded.EntryGuid)).Json)!.StateId
                .Should().Be((int)ResponseHoldState.Held);
        }

        [Fact]
        public async Task ACredentialVariableWithoutTheHookPrefixIsNeverReadAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Sneaky", receiver.BaseUrl + "/release",
                secretVariable: "PATH");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<HoldHookException>(() => service.ReleaseAsync(Decision(seeded)));

            receiver.Calls().Should().BeEmpty();
        }

        [Fact]
        public async Task RefuseCallsTheRefuseUrlWhenTheHookHasOneAndRecordsRefusedAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release",
                receiver.BaseUrl + "/refuse/[@Payload.PaymentId@]");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.RefuseAsync(Decision(seeded, "Suspected mule account"));

            result.StateId.Should().Be((int)ResponseHoldState.Refused);

            var call = receiver.Calls().Should().ContainSingle().Subject;
            call.PathAndQuery.Should().Be("/refuse/P-1");
            JObject.Parse(call.Body)["action"]!.Value<string>().Should().Be("refuse");

            var hold = ResponseHoldJson.Read((await ArchiveAsync(seeded.EntryGuid)).Json)!;
            hold.StateId.Should().Be((int)ResponseHoldState.Refused);
            hold.Legs.Should().Contain(l => l.KindId == (int)ResponseHoldLegKind.RefuseHook
                                            && l.OutcomeId == (int)ResponseHoldLegOutcome.Sent);
        }

        [Fact]
        public async Task RefuseWithoutARefuseUrlOrAnyHookIsRecordedWithoutCallingAnythingAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.RefuseAsync(Decision(seeded));

            result.StateId.Should().Be((int)ResponseHoldState.Refused);
            result.HttpStatus.Should().BeNull();
            receiver.Calls().Should().BeEmpty();

            var hold = ResponseHoldJson.Read((await ArchiveAsync(seeded.EntryGuid)).Json)!;
            hold.Decision!.Reason.Should().Be("Verified with the customer");
            (await DispatchesAsync(seeded.CaseId)).Should().BeEmpty();
        }

        [Fact]
        public async Task ADeclinedPaymentCanNeverBeReleasedOrRefusedAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held(ResponseHoldState.Declined));
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<HoldStateException>(() => service.ReleaseAsync(Decision(seeded)));
            await Assert.ThrowsAsync<HoldStateException>(() => service.RefuseAsync(Decision(seeded)));

            receiver.Calls().Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ADecisionWithoutAReasonIsInvalidAndNothingIsCalledAsync(string? reason)
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.ReleaseAsync(Decision(seeded, reason)));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ReasonNotEmpty");

            receiver.Calls().Should().BeEmpty();
        }

        [Fact]
        public async Task ACaseLockedByAnotherUserCannotHaveItsPaymentReleasedAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held(), lockedUser: "someone-else");
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<CaseLockedException>(() => service.ReleaseAsync(Decision(seeded)));

            receiver.Calls().Should().BeEmpty();
        }

        [Fact]
        public async Task ATransactionThatIsNotOnTheCaseCannotBeReleasedThroughItAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            var other = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ReleaseAsync(new CaseHoldDecisionDto
            {
                CaseId = seeded.CaseId, EntityAnalysisModelInstanceEntryGuid = other.EntryGuid, Reason = "x"
            }));

            receiver.Calls().Should().BeEmpty();
            ResponseHoldJson.Read((await ArchiveAsync(other.EntryGuid)).Json)!.StateId
                .Should().Be((int)ResponseHoldState.Held);
        }

        [Fact]
        public async Task ACustomBodyIsRenderedWithEveryValueEscapedForJsonAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release",
                body: "{\"id\":\"[@Payload.PaymentId@]\",\"why\":\"[@Decision.Reason@]\",\"by\":\"[@Decision.User@]\"}");

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.ReleaseAsync(Decision(seeded, "He said \"fine\"\nand left"));

            var body = JObject.Parse(receiver.Calls().Single().Body);
            body["why"]!.Value<string>().Should().Be("He said \"fine\"\nand left");
            body["id"]!.Value<string>().Should().Be("P-1");
        }

        [Fact]
        public async Task AGetHookCallsTheUrlWithoutABodyAsync()
        {
            using var receiver = new Receiver();
            await using var dbContext = fx.GetDbContext();
            var seeded = await SeedAsync(dbContext, fx.Seed.UserWithPermission, Held());
            await HookAsync(dbContext, seeded.WorkflowGuid, "Faster payments", receiver.BaseUrl + "/release", method: 2);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.ReleaseAsync(Decision(seeded));

            var call = receiver.Calls().Single();
            call.Method.Should().Be("GET");
            call.Body.Should().BeEmpty();
        }

        [Fact]
        public void TheToolCatalogueDeclaresTheThreeOperationsWithTheirPermissions()
        {
            var tools = ServiceToolCatalogue.All.Where(w => w.Name.StartsWith("CaseHold", StringComparison.Ordinal))
                .ToDictionary(d => d.Name);

            tools.Keys.Should().BeEquivalentTo("CaseHoldList", "CaseHoldRelease", "CaseHoldRefuse");
            tools["CaseHoldList"].Kind.Should().Be(OperationKind.Read);
            tools["CaseHoldRelease"].Kind.Should().Be(OperationKind.Write);
            tools["CaseHoldRelease"].RequiredPermissions.Should().Equal(68);
            tools["CaseHoldRefuse"].RequiredPermissions.Should().Equal(68);
            tools["CaseHoldList"].RequiredPermissions.Should().Equal(1);
        }
    }
}

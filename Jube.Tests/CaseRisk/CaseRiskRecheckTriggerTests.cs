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
using Jube.Data.Poco;
using Jube.Data.Query.MarkCaseRiskRecheckQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.Case;
using Jube.Dto.Repository.CaseNote;
using Jube.Service.Reactivity;
using Jube.Service.Repository.Case;
using Jube.Service.Repository.CaseNote;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.CaseNote;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseRiskRecheckTriggerTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly EngineJsonSerializationHelper jsonSerializationHelper = new();

        private readonly CaseRiskSeeder seeder = new(fx);
        private readonly List<int> extraCaseIds = [];
        private readonly List<string> graphKeyValues = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<GraphNode>().Where(w => graphKeyValues.Contains(w.Value)).DeleteAsync();
            await dbContext.CaseNote.Where(w => w.CaseId != null && extraCaseIds.Contains(w.CaseId.Value))
                .DeleteAsync();
            await dbContext.Case.Where(w => extraCaseIds.Contains(w.Id)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private JubeDynamicEnvironment Environment() => TestDynamicEnvironment.Create(
            new Dictionary<string, string> { ["ConnectionString"] = fx.ConnectionString });

        private static string Unique() => $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}"[..24];

        private static Task<Jube.Data.Poco.Case> ReadAsync(DbContext dbContext, int caseId) =>
            dbContext.Case.FirstAsync(w => w.Id == caseId);

        private Task<int> OpenCaseAsync(DbContext dbContext, CaseRiskSeeder.Seeded workflow, string keyValue,
            byte closedStatusId = 0) =>
            seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, closedStatusId, keyValue,
                riskCheckedDate: DateTime.UtcNow);

        private static async Task AssertMarkedAsync(DbContext dbContext, int caseId, CaseRiskTrigger trigger)
        {
            var stored = await ReadAsync(dbContext, caseId);
            stored.RiskCheckedDate.Should().BeNull();
            stored.RiskRecheckTriggerId.Should().Be((byte)trigger);
        }

        private static async Task AssertUntouchedAsync(DbContext dbContext, int caseId)
        {
            var stored = await ReadAsync(dbContext, caseId);
            stored.RiskCheckedDate.Should().NotBeNull();
            stored.RiskRecheckTriggerId.Should().BeNull();
        }

        private async Task<GraphNode> NodeAsync(DbContext dbContext, string value)
        {
            graphKeyValues.Add(value);

            var node = new GraphNode
            {
                TenantRegistryId = await dbContext.UserInTenant
                    .Where(w => w.User == fx.Seed.UserWithPermission).Select(s => s.TenantRegistryId).FirstAsync(),
                Key = "AccountId",
                Value = value,
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };

            node.Id = await dbContext.InsertWithInt64IdentityAsync(node);

            return node;
        }

        [Fact]
        public async Task TheQueryMarksOnlyTheNamedOpenCasesAndNeverAClosedOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var open = await OpenCaseAsync(dbContext, workflow, Unique());
            var suspended = await OpenCaseAsync(dbContext, workflow, Unique(), 1);
            var closed = await OpenCaseAsync(dbContext, workflow, Unique(), 3);
            var unnamed = await OpenCaseAsync(dbContext, workflow, Unique());

            var marked = await new MarkCaseRiskRecheckQuery(dbContext)
                .ExecuteAsync([open, suspended, closed, 0, open], CaseRiskTrigger.ActivationAttached);

            marked.Should().Be(2);
            await AssertMarkedAsync(dbContext, open, CaseRiskTrigger.ActivationAttached);
            await AssertMarkedAsync(dbContext, suspended, CaseRiskTrigger.ActivationAttached);
            await AssertUntouchedAsync(dbContext, closed);
            await AssertUntouchedAsync(dbContext, unnamed);
        }

        [Fact]
        public async Task TheQueryWithNoCasesDoesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();

            (await new MarkCaseRiskRecheckQuery(dbContext).ExecuteAsync([], CaseRiskTrigger.PaymentHold))
                .Should().Be(0);
            (await new MarkCaseRiskRecheckQuery(dbContext).ForGraphNodesAsync(1, [], CaseRiskTrigger.NeighbourTagged))
                .Should().Be(0);
        }

        [Fact]
        public async Task AttachingAnActivationToAnExistingCaseMarksItActivationAttachedAndLeavesOthersAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var attached = await OpenCaseAsync(dbContext, workflow, Unique());
            var other = await OpenCaseAsync(dbContext, workflow, Unique());
            var closed = await OpenCaseAsync(dbContext, workflow, Unique(), 3);

            await new MarkCaseRiskRecheckQuery(dbContext).ExecuteAsync([attached], CaseRiskTrigger.ActivationAttached);
            await new MarkCaseRiskRecheckQuery(dbContext).ExecuteAsync([closed], CaseRiskTrigger.ActivationAttached);

            await AssertMarkedAsync(dbContext, attached, CaseRiskTrigger.ActivationAttached);
            await AssertUntouchedAsync(dbContext, other);
            await AssertUntouchedAsync(dbContext, closed);
        }

        [Fact]
        public async Task ThePaymentHoldDecisionMarksOnlyTheOpenCaseOfTheHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var held = await OpenCaseAsync(dbContext, workflow, Unique());
            var other = await OpenCaseAsync(dbContext, workflow, Unique());
            var closed = await OpenCaseAsync(dbContext, workflow, Unique(), 3);

            await new MarkCaseRiskRecheckQuery(dbContext).ExecuteAsync([held], CaseRiskTrigger.PaymentHold);
            await new MarkCaseRiskRecheckQuery(dbContext).ExecuteAsync([closed], CaseRiskTrigger.PaymentHold);

            await AssertMarkedAsync(dbContext, held, CaseRiskTrigger.PaymentHold);
            await AssertUntouchedAsync(dbContext, other);
            await AssertUntouchedAsync(dbContext, closed);
        }

        [Fact]
        public async Task ClosingACaseMarksTheOtherOpenCasesOnTheSameKeyLinkedKeyChangedButNotItselfAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var otherWorkflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, user);
            var key = Unique();
            var closing = await OpenCaseAsync(dbContext, workflow, key);
            var sibling = await OpenCaseAsync(dbContext, workflow, key);
            var suspendedSibling = await OpenCaseAsync(dbContext, workflow, key, 1);
            var alreadyClosed = await OpenCaseAsync(dbContext, workflow, key, 3);
            var otherKey = await OpenCaseAsync(dbContext, workflow, Unique());
            var otherFlow = await OpenCaseAsync(dbContext, otherWorkflow, key);

            var service = await CaseService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), Environment(), jsonSerializationHelper, TestLog.NoOp);

            await service.UpdateAsync(new CaseDto
            {
                Id = closing,
                CaseWorkflowStatusGuid = workflow.Neutral,
                ClosedStatusId = 3,
                Rating = 0,
                Payload = "{}"
            });

            (await ReadAsync(dbContext, closing)).ClosedStatusId.Should().Be(3);
            await AssertUntouchedAsync(dbContext, closing);
            await AssertMarkedAsync(dbContext, sibling, CaseRiskTrigger.LinkedKeyChanged);
            await AssertMarkedAsync(dbContext, suspendedSibling, CaseRiskTrigger.LinkedKeyChanged);
            await AssertUntouchedAsync(dbContext, alreadyClosed);
            await AssertUntouchedAsync(dbContext, otherKey);
            await AssertUntouchedAsync(dbContext, otherFlow);
        }

        [Fact]
        public async Task AnUpdateThatDoesNotCloseTheCaseMarksNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, user);
            var key = Unique();
            var edited = await OpenCaseAsync(dbContext, workflow, key);
            var sibling = await OpenCaseAsync(dbContext, workflow, key);

            var service = await CaseService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), Environment(), jsonSerializationHelper, TestLog.NoOp);

            await service.UpdateAsync(new CaseDto
            {
                Id = edited,
                CaseWorkflowStatusGuid = workflow.Neutral,
                ClosedStatusId = 0,
                Rating = 0,
                Payload = "{}"
            });

            await AssertUntouchedAsync(dbContext, sibling);
        }

        [Fact]
        public async Task RecordingAFindingMarksTheNotesOpenCaseFindingRecordedAndNoOtherCaseAsync()
        {
            var user = fx.Seed.UserWithPermission;
            await using var seed = await CaseNoteSeed.CreateAsync(fx, user);
            await using var dbContext = fx.GetDbContext();

            var sibling = await dbContext.InsertWithInt32IdentityAsync(new Jube.Data.Poco.Case
            {
                CaseWorkflowGuid = seed.CaseWorkflowGuid,
                CaseWorkflowStatusGuid = seed.CaseWorkflowStatusGuid,
                CaseKey = seed.CaseKey,
                CaseKeyValue = seed.CaseKeyValue,
                Json = "{}",
                CreatedDate = DateTime.UtcNow,
                ClosedStatusId = 0,
                RiskCheckedDate = DateTime.UtcNow
            });
            extraCaseIds.Add(sibling);

            await dbContext.Case.Where(w => w.Id == seed.CaseId).Set(s => s.RiskCheckedDate, DateTime.UtcNow)
                .UpdateAsync();

            var noteId = await seed.CreateNoteAsync(user);

            var service = await CaseNoteService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), Environment(), jsonSerializationHelper, TestLog.NoOp);

            await service.SetFindingAsync(new CaseNoteFindingRequestDto { Id = noteId, IsFinding = true });

            await AssertMarkedAsync(dbContext, seed.CaseId, CaseRiskTrigger.FindingRecorded);
            await AssertUntouchedAsync(dbContext, sibling);
        }

        [Fact]
        public async Task ChangingTheTypologySeverityMarksTheOpenCasesOfTheWorkflowTypologyChangedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext, 3);
            var otherWorkflow = await seeder.SeedWorkflowAsync(dbContext, 3);
            var open = await OpenCaseAsync(dbContext, workflow, Unique());
            var suspended = await OpenCaseAsync(dbContext, workflow, Unique(), 1);
            var closed = await OpenCaseAsync(dbContext, workflow, Unique(), 3);
            var elsewhere = await OpenCaseAsync(dbContext, otherWorkflow, Unique());

            var model = await dbContext.CaseWorkflow.FirstAsync(w => w.Guid == workflow.WorkflowGuid);
            model.TypologySeverityId = 5;

            await new CaseWorkflowRepository(dbContext, fx.Seed.UserWithPermission).UpdateAsync(model);

            await AssertMarkedAsync(dbContext, open, CaseRiskTrigger.TypologyChanged);
            await AssertMarkedAsync(dbContext, suspended, CaseRiskTrigger.TypologyChanged);
            await AssertUntouchedAsync(dbContext, closed);
            await AssertUntouchedAsync(dbContext, elsewhere);
        }

        [Fact]
        public async Task AnotherRiskInputChangeMarksTheOpenCasesModelChangedAndAnIrrelevantEditMarksNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var open = await OpenCaseAsync(dbContext, workflow, Unique());
            var closed = await OpenCaseAsync(dbContext, workflow, Unique(), 3);
            var repository = new CaseWorkflowRepository(dbContext, fx.Seed.UserWithPermission);

            var model = await dbContext.CaseWorkflow.FirstAsync(w => w.Guid == workflow.WorkflowGuid);
            model.Name = $"{DatabaseFixture.Prefix}Renamed{Guid.NewGuid():N}"[..40];
            model = await repository.UpdateAsync(model);

            await AssertUntouchedAsync(dbContext, open);

            model.ValueField = "Amount";
            await repository.UpdateAsync(model);

            await AssertMarkedAsync(dbContext, open, CaseRiskTrigger.ModelChanged);
            await AssertUntouchedAsync(dbContext, closed);
        }

        [Fact]
        public async Task GraphNodesThatMovedMarkOnlyTheOpenCasesOnThoseNodesNeighbourTaggedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var moved = Unique();
            var still = Unique();
            var closedValue = Unique();
            var movedNode = await NodeAsync(dbContext, moved);
            await NodeAsync(dbContext, still);
            var closedNode = await NodeAsync(dbContext, closedValue);
            var onMoved = await OpenCaseAsync(dbContext, workflow, moved);
            var onStill = await OpenCaseAsync(dbContext, workflow, still);
            var onClosed = await OpenCaseAsync(dbContext, workflow, closedValue, 3);

            var marked = await new MarkCaseRiskRecheckQuery(dbContext).ForGraphNodesAsync(movedNode.TenantRegistryId,
                [movedNode.Id, closedNode.Id], CaseRiskTrigger.NeighbourTagged);

            marked.Should().Be(1);
            await AssertMarkedAsync(dbContext, onMoved, CaseRiskTrigger.NeighbourTagged);
            await AssertUntouchedAsync(dbContext, onStill);
            await AssertUntouchedAsync(dbContext, onClosed);
        }

        [Fact]
        public async Task GraphNodesOfAnotherTenantMarkNoCasesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var value = Unique();
            var node = await NodeAsync(dbContext, value);
            var open = await OpenCaseAsync(dbContext, workflow, value);

            var marked = await new MarkCaseRiskRecheckQuery(dbContext).ForGraphNodesAsync(
                node.TenantRegistryId + 1_000_000, [node.Id], CaseRiskTrigger.NeighbourTagged);

            marked.Should().Be(0);
            await AssertUntouchedAsync(dbContext, open);
        }

        [Fact]
        public async Task TheEventRepositoryRefusesANonCanonicalStatusNameAndAcceptsAGuidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await OpenCaseAsync(dbContext, workflow, Unique());
            var repository = new CaseEventRepository(dbContext, fx.Seed.UserWithPermission);

            CaseEvent Event(string before, string after) => new()
            {
                CaseId = caseId,
                CaseEventTypeId = 9,
                CaseKey = "AccountId",
                CaseKeyValue = "k",
                Before = before,
                After = after
            };

            var refused = () => repository.InsertAsync(Event("Open", "Escalated"));

            await refused.Should().ThrowAsync<InvalidOperationException>();

            var accepted = await repository.InsertAsync(Event(workflow.Neutral.ToString(),
                workflow.Confirmed.ToString()));

            accepted.Id.Should().BeGreaterThan(0);
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(1);
        }
    }
}

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
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Models;
using Jube.Service.Repository.AskJoobyAnalyticsInstance;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Xunit;
using Jube.Service.Repository.AskJoobyAnalyticsInstance.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AskJoobyAnalyticsInstanceServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private async Task<AskJoobyAnalyticsInstanceDto> CreateAsync(string? user = null, string? name = null)
        {
            var service = await InstanceServiceAsync(user ?? Permitted);

            var saved = await service.InsertAsync(
                new AskJoobyAnalyticsInstanceDto { Name = name ?? UniqueName("Inst") });
            TrackInstance(saved.Id);

            return saved;
        }

        [Fact]
        public async Task InsertPersistsUnallocatedUnlivedVersionOneInTheCallersTenantAsync()
        {
            var saved = await CreateAsync();

            saved.Id.Should().BeGreaterThan(0);
            saved.Guid.Should().NotBe(Guid.Empty);
            saved.Version.Should().Be(1);
            saved.EntityAnalysisModelId.Should().BeNull();
            saved.Active.Should().BeFalse();
            saved.CreatedUser.Should().Be(Permitted);

            var row = await Db.AskJoobyAnalyticsInstance.FirstAsync(w => w.Id == saved.Id);
            row.TenantRegistryId.Should().Be(TenantA);
        }

        [Fact]
        public async Task InsertIgnoresASuppliedAllocationAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var service = await InstanceServiceAsync(Permitted);

            var saved = await service.InsertAsync(new AskJoobyAnalyticsInstanceDto
            {
                Name = UniqueName("NoAlloc"),
                EntityAnalysisModelId = modelId
            });
            TrackInstance(saved.Id);

            saved.EntityAnalysisModelId.Should().BeNull();
        }

        [Fact]
        public async Task EveryOperationRefusesAnUnpermittedUserAsync()
        {
            var existing = await CreateAsync();
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var service = await InstanceServiceAsync(Unpermitted);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(existing.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.InsertAsync(new AskJoobyAnalyticsInstanceDto { Name = UniqueName("Denied") }));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.UpdateAsync(new AskJoobyAnalyticsInstanceDto { Id = existing.Id, Name = existing.Name }));

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(existing.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.AllocateToModelAsync(existing.Id, modelId));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeallocateAsync(existing.Id));
        }

        [Fact]
        public async Task PromotePermissionAloneCannotManageInstancesAndAnalyticsAloneCannotAllocateAsync()
        {
            var existing = await CreateAsync();
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var promoteOnly = await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.PromoteModel);
            var analyticsOnly = await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.Analytics);

            var promoteService = await InstanceServiceAsync(promoteOnly);
            await Assert.ThrowsAsync<ForbiddenException>(() => promoteService.GetByIdAsync(existing.Id));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                promoteService.InsertAsync(new AskJoobyAnalyticsInstanceDto { Name = UniqueName("Denied") }));

            var analyticsService = await InstanceServiceAsync(analyticsOnly);
            (await analyticsService.GetByIdAsync(existing.Id)).Should().NotBeNull();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                analyticsService.AllocateToModelAsync(existing.Id, modelId));

            await Assert.ThrowsAsync<ForbiddenException>(() => analyticsService.DeallocateAsync(existing.Id));

            var allocated = await promoteService.AllocateToModelAsync(existing.Id, modelId);
            allocated.EntityAnalysisModelId.Should().Be(modelId);
        }

        [Fact]
        public async Task GoingLiveOrChangingOutputFlagsNeedsThePromotePermissionAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var analyticsOnly = await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.Analytics);
            var analyticsService = await InstanceServiceAsync(analyticsOnly);

            var refused = await Assert.ThrowsAsync<ForbiddenException>(() =>
                analyticsService.InsertAsync(new AskJoobyAnalyticsInstanceDto
                    { Name = UniqueName("Live"), ResponsePayload = true }));

            refused.RequiredSpecifications.Should().Equal(AdvancedAnalyticsPermissionSpecifications.PromoteModel);

            var instance = await CreateAsync();
            var full = await InstanceServiceAsync(Permitted);
            await full.AllocateToModelAsync(instance.Id, modelId);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                analyticsService.UpdateAsync(new AskJoobyAnalyticsInstanceDto
                    { Id = instance.Id, Name = instance.Name, Active = true }));

            var live = await full.UpdateAsync(new AskJoobyAnalyticsInstanceDto
                { Id = instance.Id, Name = instance.Name, Active = true });
            live.Active.Should().BeTrue();

            var renamed = await analyticsService.UpdateAsync(new AskJoobyAnalyticsInstanceDto
                { Id = instance.Id, Name = UniqueName("Renamed"), Active = true });
            renamed.Active.Should().BeTrue();
            renamed.EntityAnalysisModelId.Should().Be(modelId);
        }

        [Fact]
        public async Task AnInstanceCannotBeLiveWhileUnallocatedAsync()
        {
            var instance = await CreateAsync();
            var service = await InstanceServiceAsync(Permitted);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.UpdateAsync(
                new AskJoobyAnalyticsInstanceDto { Id = instance.Id, Name = instance.Name, Active = true }));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ActiveRequiresAllocation");
        }

        [Fact]
        public async Task NamesAreRequiredBoundedAndUniqueIgnoringCaseAsync()
        {
            var first = await CreateAsync();
            var service = await InstanceServiceAsync(Permitted);

            var blank = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(new AskJoobyAnalyticsInstanceDto { Name = " " }));

            blank.Result.Errors.Should().Contain(e => e.ErrorCode == "NameNotEmpty");

            var tooLong = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(new AskJoobyAnalyticsInstanceDto { Name = new string('x', 257) }));

            tooLong.Result.Errors.Should().Contain(e => e.ErrorCode == "NameMaximumLength");

            var duplicate = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(new AskJoobyAnalyticsInstanceDto { Name = first.Name!.ToUpperInvariant() }));

            duplicate.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
        }

        [Fact]
        public async Task ConcurrentCreatesOfOneNameProduceExactlyOneInstanceAsync()
        {
            var name = UniqueName("Race");
            await using var dbA = Fixture.GetDbContext();
            await using var dbB = Fixture.GetDbContext();
            var serviceA = await InstanceServiceAsync(Permitted, db: dbA);
            var serviceB = await InstanceServiceAsync(Permitted, db: dbB);

            var results = await Task.WhenAll(
                AttemptAsync(serviceA, name),
                AttemptAsync(serviceB, name));

            results.Count(r => r).Should().Be(1);
            (await Db.AskJoobyAnalyticsInstance.CountAsync(w => w.Name == name && w.Deleted == 0)).Should().Be(1);

            return;

            async Task<bool> AttemptAsync(AskJoobyAnalyticsInstanceService service, string instanceName)
            {
                try
                {
                    var saved = await service.InsertAsync(new AskJoobyAnalyticsInstanceDto { Name = instanceName });
                    TrackInstance(saved.Id);

                    return true;
                }
                catch (DtoValidationException)
                {
                    return false;
                }
            }
        }

        [Fact]
        public async Task UpdateRenamesWithoutChangingVersionAndNeverTouchesTheAllocationAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await CreateAsync();
            var service = await InstanceServiceAsync(Permitted);
            await service.AllocateToModelAsync(instance.Id, modelId);

            var otherModel = await NewEntityAnalysisModelAsync(TenantA);

            var updated = await service.UpdateAsync(new AskJoobyAnalyticsInstanceDto
            {
                Id = instance.Id,
                Name = UniqueName("Renamed"),
                EntityAnalysisModelId = otherModel
            });

            updated.EntityAnalysisModelId.Should().Be(modelId);
            updated.Version.Should().Be(instance.Version);
            updated.Name.Should().NotBe(instance.Name);
        }

        [Fact]
        public async Task TenantsCannotSeeOrTouchEachOthersInstancesAsync()
        {
            var instance = await CreateAsync();
            var modelB = await NewEntityAnalysisModelAsync(TenantB);
            var other = await InstanceServiceAsync(OtherTenant);

            (await other.GetByIdAsync(instance.Id)).Should().BeNull();
            (await other.ListAsync(200)).Items.Should().NotContain(i => i.Id == instance.Id);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                other.UpdateAsync(new AskJoobyAnalyticsInstanceDto { Id = instance.Id, Name = UniqueName("Steal") }));

            await Assert.ThrowsAsync<NotFoundException>(() => other.DeleteAsync(instance.Id));
            await Assert.ThrowsAsync<NotFoundException>(() => other.AllocateToModelAsync(instance.Id, modelB));
            await Assert.ThrowsAsync<NotFoundException>(() => other.DeallocateAsync(instance.Id));
        }

        [Fact]
        public async Task ListPagesByIdAndFiltersByModelAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var one = await CreateAsync();
            var two = await CreateAsync();
            var service = await InstanceServiceAsync(Permitted);
            await service.AllocateToModelAsync(two.Id, modelId);

            var all = (await service.ListAsync(200)).Items;
            all.Select(i => i.Id).Should().BeInAscendingOrder();
            all.Should().Contain(i => i.Id == one.Id).And.Contain(i => i.Id == two.Id);

            var page = await service.ListAsync(1, one.Id - 1);
            page.Items.Should().HaveCount(1);
            page.Items[0].Id.Should().BeGreaterThan(one.Id - 1);

            var filtered = await service.ListAsync(200, null, modelId);
            filtered.Items.Should().ContainSingle().Which.Id.Should().Be(two.Id);

            (await service.ListAsync(10_000)).Items.Count.Should().BeLessThanOrEqualTo(200);
        }

        [Fact]
        public async Task AllocateValidatesTheModelBelongsToTheTenantAndIsNotDeletedAsync()
        {
            var instance = await CreateAsync();
            var service = await InstanceServiceAsync(Permitted);
            var foreign = await NewEntityAnalysisModelAsync(TenantB);
            var deleted = await NewEntityAnalysisModelAsync(TenantA, true);

            foreach (var modelId in new[] { foreign, deleted, int.MaxValue - 1, 0, -5 })
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.AllocateToModelAsync(instance.Id, modelId));

                ex.Result.Errors.Should().NotBeEmpty();
            }

            (await Db.AskJoobyAnalyticsInstance.FirstAsync(w => w.Id == instance.Id)).EntityAnalysisModelId
                .Should().BeNull();
        }

        [Fact]
        public async Task AllocatePublishesOneUpdatedEventAndRepeatingItChangesNothingAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await CreateAsync();
            var bus = new CapturingBus();
            var service = await InstanceServiceAsync(Permitted, bus: bus);

            var allocated = await service.AllocateToModelAsync(instance.Id, modelId);
            var again = await service.AllocateToModelAsync(instance.Id, modelId);

            allocated.EntityAnalysisModelId.Should().Be(modelId);
            again.Version.Should().Be(allocated.Version);
            bus.Published.Should().ContainSingle();
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Updated);
            bus.Published[0].Area.Should().Be(AdvancedAnalyticsChangeAreas.Instance);
            bus.Published[0].EntityId.Should().Be(instance.Id);
            bus.Published[0].TenantRegistryId.Should().Be(TenantA);
        }

        [Fact]
        public async Task AnAllocatedInstanceCannotMoveToAnotherModelUntilDeallocatedAsync()
        {
            var first = await NewEntityAnalysisModelAsync(TenantA);
            var second = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await CreateAsync();
            var service = await InstanceServiceAsync(Permitted);
            await service.AllocateToModelAsync(instance.Id, first);

            var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
                service.AllocateToModelAsync(instance.Id, second));
            conflict.Reason.Should().Be("AlreadyAllocated");

            await service.DeallocateAsync(instance.Id);
            var moved = await service.AllocateToModelAsync(instance.Id, second);
            moved.EntityAnalysisModelId.Should().Be(second);
        }

        [Fact]
        public async Task DeallocateClearsTheModelAndLiveFlagAndIsIdempotentAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await CreateAsync();
            var bus = new CapturingBus();
            var service = await InstanceServiceAsync(Permitted, bus: bus);
            await service.AllocateToModelAsync(instance.Id, modelId);

            await service.UpdateAsync(new AskJoobyAnalyticsInstanceDto
                { Id = instance.Id, Name = instance.Name, Active = true });

            var before = bus.Published.Count;

            var cleared = await service.DeallocateAsync(instance.Id);
            var repeated = await service.DeallocateAsync(instance.Id);

            cleared.EntityAnalysisModelId.Should().BeNull();
            cleared.Active.Should().BeFalse();
            repeated.Version.Should().Be(cleared.Version);
            bus.Published.Count.Should().Be(before + 1);
        }

        [Fact]
        public async Task ConcurrentAllocationsOfOneInstanceToDifferentModelsLeaveExactlyOneAsync()
        {
            var first = await NewEntityAnalysisModelAsync(TenantA);
            var second = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await CreateAsync();
            await using var dbA = Fixture.GetDbContext();
            await using var dbB = Fixture.GetDbContext();
            var serviceA = await InstanceServiceAsync(Permitted, db: dbA);
            var serviceB = await InstanceServiceAsync(Permitted, db: dbB);

            var outcomes = await Task.WhenAll(
                TryAsync(() => serviceA.AllocateToModelAsync(instance.Id, first)),
                TryAsync(() => serviceB.AllocateToModelAsync(instance.Id, second)));

            outcomes.Count(o => o).Should().Be(1);
            var row = await Db.AskJoobyAnalyticsInstance.FirstAsync(w => w.Id == instance.Id);
            row.EntityAnalysisModelId.Should().BeOneOf(first, second);

            return;

            static async Task<bool> TryAsync(Func<Task<AskJoobyAnalyticsInstanceDto>> action)
            {
                try
                {
                    await action();

                    return true;
                }
                catch (ConflictException)
                {
                    return false;
                }
            }
        }

        [Fact]
        public async Task DeleteSoftDeletesHidesTheRowAndPublishesDeletedAsync()
        {
            var instance = await CreateAsync();
            var bus = new CapturingBus();
            var service = await InstanceServiceAsync(Permitted, bus: bus);

            await service.DeleteAsync(instance.Id);

            (await service.GetByIdAsync(instance.Id)).Should().BeNull();
            var row = await Db.AskJoobyAnalyticsInstance.FirstAsync(w => w.Id == instance.Id);
            row.Deleted.Should().Be(1);
            row.DeletedUser.Should().Be(Permitted);
            bus.Published.Should().ContainSingle(e => e.Kind == ServiceChangeKind.Deleted);
            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(instance.Id));
        }

        [Fact]
        public async Task DeletingAnAllocatedInstanceNeedsThePromotePermissionAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await CreateAsync();
            var full = await InstanceServiceAsync(Permitted);
            await full.AllocateToModelAsync(instance.Id, modelId);
            var analyticsOnly = await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.Analytics);
            var restricted = await InstanceServiceAsync(analyticsOnly);

            await Assert.ThrowsAsync<ForbiddenException>(() => restricted.DeleteAsync(instance.Id));
            await full.DeleteAsync(instance.Id);
        }

        [Fact]
        public async Task ServiceRefusesUsersWithoutAnIdentityOrATenantAsync()
        {
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => InstanceServiceAsync(null));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => InstanceServiceAsync(" "));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => InstanceServiceAsync(Fixture.Seed.UserNoTenant));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => InstanceServiceAsync(Fixture.Seed.UnknownUser));
        }

        [Fact]
        public async Task PermissionDeniedIsAWarningNotAnErrorAsync()
        {
            var log = new TestLog();
            var service = await InstanceServiceAsync(Unpermitted, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(Unpermitted));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task EachCallEmitsASpanADurationMeasurementAndOneAuditLineAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);
            var audit = new TestLog();
            var service = await InstanceServiceAsync(Permitted, auditLog: audit);

            var saved = await service.InsertAsync(new AskJoobyAnalyticsInstanceDto { Name = UniqueName("Obs") });
            TrackInstance(saved.Id);

            var span = activities.Should().ContainSingle(a => a.OperationName == "AskJoobyAnalyticsInstance.Create")
                .Subject;
            span.GetTagItem("jube.outcome").Should().Be("ok");
            span.GetTagItem("jube.entity.id").Should().Be(saved.Id);
            collector.GetMeasurementSnapshot().Should().ContainSingle(m => (string)m.Tags["operation"]! == "Create");
            audit.Entries.Should().ContainSingle();
            audit.Entries[0].Message.Should().Contain("op=Create").And.Contain($"entityId={saved.Id}");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNoChangeEventAsync()
        {
            var bus = new CapturingBus();
            var service = await InstanceServiceAsync(Permitted, bus: bus);

            await service.ListAsync();

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(new AskJoobyAnalyticsInstanceDto { Name = "" }));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            var service = await InstanceServiceAsync(Permitted);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ListAsync(token: cts.Token));
        }
    }
}
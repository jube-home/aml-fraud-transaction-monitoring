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
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Service.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Service.Exceptions.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.ReprocessingPermissions;

[Trait("Category", "Service")]
[Collection("Database")]
public sealed class ReprocessingInstancePermissionSplitTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const int EditRule = 26;
    private const int TriggerRun = 73;
    private const int StopRun = 74;

    private static readonly IStringLocalizerFactory localizers =
        new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
            NullLoggerFactory.Instance);

    private readonly ReprocessingPermissionUsers users = new(fx);
    private readonly List<int> createdModelIds = [];
    private readonly List<int> createdRuleIds = [];

    public static IEnumerable<object[]> ReadHolders() =>
        [[EditRule], [TriggerRun], [StopRun]];

    public static IEnumerable<object[]> NonTriggerHolders() =>
        [[EditRule], [StopRun]];

    public static IEnumerable<object[]> NonStopHolders() =>
        [[EditRule], [TriggerRun]];

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var dbContext = fx.GetDbContext();

        foreach (var id in createdRuleIds)
        {
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRuleInstance>()
                .Where(w => w.EntityAnalysisModelReprocessingRuleId == id).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRule>().Where(w => w.Id == id)
                .DeleteAsync();
        }

        foreach (var modelId in createdModelIds)
        {
            await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
        }

        await users.DisposeAsync();
    }

    private static Task<EntityAnalysisModelReprocessingRuleInstanceService> BuildServiceAsync(DbContext dbContext,
        string userName)
    {
        return EntityAnalysisModelReprocessingRuleInstanceService.CreateAsync(dbContext, userName, TestLog.NoOp,
            localizers, new NullServiceChangeBus(), TestLog.NoOp);
    }

    private async Task<int> CreateRuleAsync(DbContext dbContext)
    {
        var model = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
            new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                Locked = 0, Deleted = 0
            });

        createdModelIds.Add(model.Id);

        var rule = await new EntityAnalysisModelReprocessingRuleRepository(dbContext, fx.Seed.UserWithPermission)
            .InsertAsync(new Data.Poco.EntityAnalysisModelReprocessingRule
            {
                EntityAnalysisModelId = model.Id,
                Name = $"{DatabaseFixture.Prefix}Rule{Guid.NewGuid():N}"[..40],
                Priority = 1, Active = 1, Locked = 0, RuleScriptTypeId = 1,
                BuilderRuleScript = "If (1 > 0) Then\n   Return True\nEnd If",
                Json = "{\"valid\":true,\"condition\":\"AND\",\"rules\":[]}",
                CoderRuleScript = "Return True", ReprocessingSample = 100, ReprocessingValue = 1,
                ReprocessingInterval = "d"
            });

        createdRuleIds.Add(rule.Id);

        return rule.Id;
    }

    private static EntityAnalysisModelReprocessingRuleInstanceDto NewDto(int ruleId)
    {
        return new EntityAnalysisModelReprocessingRuleInstanceDto
        {
            EntityAnalysisModelReprocessingRuleId = ruleId, StatusId = 0, AvailableCount = 0, SampledCount = 0,
            MatchedCount = 0, ProcessedCount = 0, ErrorCount = 0
        };
    }

    private async Task<(int RuleId, EntityAnalysisModelReprocessingRuleInstanceDto Instance)> SeedRunAsync()
    {
        await using var dbContext = fx.GetDbContext();
        var ruleId = await CreateRuleAsync(dbContext);
        var writer = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
        var instance = await writer.InsertByExistingUpdateUncompletedAsync(NewDto(ruleId));

        return (ruleId, instance);
    }

    private async Task<int> LiveInstanceCountAsync(int ruleId)
    {
        await using var dbContext = fx.GetDbContext();

        return await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRuleInstance>()
            .CountAsync(w => w.EntityAnalysisModelReprocessingRuleId == ruleId &&
                             (w.Deleted == 0 || w.Deleted == null));
    }

    [Fact]
    public async Task TheTriggerPermissionAloneCanStartAValidatedRunButCannotStopItAsync()
    {
        var trigger = await users.CreateAsync("Trigger", TriggerRun);
        await using var dbContext = fx.GetDbContext();
        var ruleId = await CreateRuleAsync(dbContext);
        var service = await BuildServiceAsync(dbContext, trigger);

        (await service.ValidateAsync(NewDto(ruleId))).Should().NotBeNull();

        var run = await service.InsertByExistingUpdateUncompletedAsync(NewDto(ruleId));
        run.StatusId.Should().Be(0);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(run.Id));
        (await LiveInstanceCountAsync(ruleId)).Should().Be(1);
    }

    [Fact]
    public async Task TheTriggerPermissionAloneCanCreateAPlainInstanceAsync()
    {
        var trigger = await users.CreateAsync("TriggerPlain", TriggerRun);
        await using var dbContext = fx.GetDbContext();
        var ruleId = await CreateRuleAsync(dbContext);
        var service = await BuildServiceAsync(dbContext, trigger);

        var created = await service.InsertAsync(NewDto(ruleId));

        created.Id.Should().BeGreaterThan(0);
        created.StatusId.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(NonTriggerHolders))]
    public async Task AHolderWithoutTheTriggerPermissionCannotStartValidateOrUpdateARunAndNothingIsWrittenAsync(
        int spec)
    {
        var (ruleId, seeded) = await SeedRunAsync();
        var holder = await users.CreateAsync("NoTrigger" + spec, spec);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, holder);
        var before = await LiveInstanceCountAsync(ruleId);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(NewDto(ruleId)));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertByExistingUpdateUncompletedAsync(NewDto(ruleId)));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ValidateAsync(NewDto(ruleId)));

        var update = NewDto(ruleId);
        update.Id = seeded.Id;
        update.Version = seeded.Version;
        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(update));

        (await LiveInstanceCountAsync(ruleId)).Should().Be(before);
    }

    [Fact]
    public async Task TheStopPermissionAloneCanDeleteARunAsync()
    {
        var (ruleId, seeded) = await SeedRunAsync();
        var stopper = await users.CreateAsync("Stop", StopRun);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, stopper);

        await service.DeleteAsync(seeded.Id);

        (await LiveInstanceCountAsync(ruleId)).Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(NonStopHolders))]
    public async Task AHolderWithoutTheStopPermissionCannotDeleteARunAndTheRunSurvivesAsync(int spec)
    {
        var (ruleId, seeded) = await SeedRunAsync();
        var holder = await users.CreateAsync("NoStop" + spec, spec);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, holder);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seeded.Id));

        (await LiveInstanceCountAsync(ruleId)).Should().Be(1);
    }

    [Fact]
    public async Task AHolderOfOnlyTheStopPermissionCannotStartAnotherRunAsync()
    {
        var (ruleId, seeded) = await SeedRunAsync();
        var stopper = await users.CreateAsync("StopOnly", StopRun);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, stopper);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertByExistingUpdateUncompletedAsync(NewDto(ruleId)));

        (await LiveInstanceCountAsync(ruleId)).Should().Be(1);
        seeded.Id.Should().BeGreaterThan(0);
    }

    [Theory]
    [MemberData(nameof(ReadHolders))]
    public async Task EachOfTheThreePermissionsCanReadTheRunsAsync(int spec)
    {
        var (ruleId, seeded) = await SeedRunAsync();
        var holder = await users.CreateAsync("Read" + spec, spec);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, holder);

        (await service.GetByIdAsync(seeded.Id)).Should().NotBeNull();
        (await service.GetByEntityAnalysisModelReprocessingIdAsync(ruleId)).Should()
            .Contain(d => d.Id == seeded.Id);
        (await service.GetAsync()).Should().Contain(d => d.Id == seeded.Id);
    }

    [Fact]
    public async Task ARoleHoldingNoneOfTheThreeCannotReadTheRunsAsync()
    {
        var (ruleId, seeded) = await SeedRunAsync();
        var holder = await users.CreateAsync("None", 1);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, holder);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seeded.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByEntityAnalysisModelReprocessingIdAsync(ruleId));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
    }

    [Fact]
    public void TheServiceDeclaresEachVerbWithItsOwnPermissionAndReadsAsAnyOfTheThree()
    {
        EntityAnalysisModelReprocessingRuleInstanceService.TriggerPermissions.Should().Equal(TriggerRun);
        EntityAnalysisModelReprocessingRuleInstanceService.StopPermissions.Should().Equal(StopRun);
        EntityAnalysisModelReprocessingRuleInstanceService.RequiredPermissions.Should()
            .BeEquivalentTo(new[] { EditRule, TriggerRun, StopRun });
    }
}

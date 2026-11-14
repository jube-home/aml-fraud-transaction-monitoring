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
using Jube.Dto.EntityAnalysisModelReprocessingRule;
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Service.EntityAnalysisModelReprocessingRule;
using Jube.Service.Exceptions.EntityAnalysisModelReprocessingRule;
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
public sealed class ReprocessingRulePermissionSplitTests(DatabaseFixture fx) : IAsyncLifetime
{
    private const int EditRule = 26;
    private const int TriggerRun = 73;
    private const int StopRun = 74;

    private static readonly IStringLocalizerFactory localizers =
        new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
            NullLoggerFactory.Instance);

    private readonly ReprocessingPermissionUsers users = new(fx);
    private readonly List<int> createdIds = [];
    private readonly List<int> createdModelIds = [];

    public static IEnumerable<object[]> ReadHolders() =>
        [[EditRule], [TriggerRun], [StopRun]];

    public static IEnumerable<object[]> NonEditHolders() =>
        [[TriggerRun], [StopRun]];

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var dbContext = fx.GetDbContext();

        foreach (var id in createdIds)
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

    private static Task<EntityAnalysisModelReprocessingRuleService> BuildServiceAsync(DbContext dbContext,
        string userName)
    {
        return EntityAnalysisModelReprocessingRuleService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
            new NullServiceChangeBus(), TestLog.NoOp);
    }

    private async Task<int> CreateModelAsync(DbContext dbContext)
    {
        var saved = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
            new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                Locked = 0, Deleted = 0
            });

        createdModelIds.Add(saved.Id);

        return saved.Id;
    }

    private static EntityAnalysisModelReprocessingRuleDto NewDto(int modelId, string tag)
    {
        return new EntityAnalysisModelReprocessingRuleDto
        {
            EntityAnalysisModelId = modelId,
            Name = $"{DatabaseFixture.Prefix}{tag}{Guid.NewGuid():N}"[..40],
            Priority = 1,
            BuilderRuleScript = "If (1 > 0) Then\n   Return True\nEnd If",
            Json = "{\"valid\":true,\"condition\":\"AND\",\"rules\":[]}",
            CoderRuleScript = "Return True",
            RuleScriptTypeId = 1,
            ReprocessingSample = 100,
            ReprocessingValue = 1,
            ReprocessingInterval = "d"
        };
    }

    private async Task<(int ModelId, Data.Poco.EntityAnalysisModelReprocessingRule Rule)> SeedRuleAsync()
    {
        await using var dbContext = fx.GetDbContext();
        var modelId = await CreateModelAsync(dbContext);
        var writer = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
        var rule = await writer.InsertAsync(NewDto(modelId, "Seed"));
        createdIds.Add(rule.Id);

        return (modelId, rule);
    }

    private async Task<int> RuleCountAsync(int modelId)
    {
        await using var dbContext = fx.GetDbContext();

        return await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRule>()
            .CountAsync(w => w.EntityAnalysisModelId == modelId && (w.Deleted == 0 || w.Deleted == null));
    }

    [Fact]
    public async Task TheEditPermissionAloneCanCreateValidateUpdateAndDeleteTheRuleAsync()
    {
        var (modelId, seeded) = await SeedRuleAsync();
        var editor = await users.CreateAsync("Edit", EditRule);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, editor);

        var created = await service.InsertAsync(NewDto(modelId, "Created"));
        createdIds.Add(created.Id);

        (await service.ValidateAsync(NewDto(modelId, "Validated"))).Should().NotBeNull();

        var update = NewDto(modelId, "Updated");
        update.Id = created.Id;
        update.Version = created.Version ?? 1;
        var updated = await service.UpdateAsync(update);
        createdIds.Add(updated.Id);

        await service.DeleteAsync(seeded.Id);

        (await service.GetByIdAsync(seeded.Id)).Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(NonEditHolders))]
    public async Task AHolderOfOnlyTheTriggerOrStopPermissionCannotEditTheRuleAndNothingIsWrittenAsync(int spec)
    {
        var (modelId, seeded) = await SeedRuleAsync();
        var holder = await users.CreateAsync("NoEdit" + spec, spec);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, holder);
        var before = await RuleCountAsync(modelId);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(NewDto(modelId, "Denied")));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ValidateAsync(NewDto(modelId, "Denied")));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ParseRuleAsync(NewDto(modelId, "Denied")));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ExecuteAsync(NewDto(modelId, "Denied"), new InvocationContextDto()));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.BuildRuleFromBuilderJsonAsync(modelId, "{}"));

        var update = NewDto(modelId, "Denied");
        update.Id = seeded.Id;
        update.Version = seeded.Version ?? 1;
        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(update));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seeded.Id));

        (await RuleCountAsync(modelId)).Should().Be(before);
    }

    [Theory]
    [MemberData(nameof(ReadHolders))]
    public async Task EachOfTheThreePermissionsCanReadTheRuleAndListTheRulesOfAModelAsync(int spec)
    {
        var (modelId, seeded) = await SeedRuleAsync();
        var holder = await users.CreateAsync("Read" + spec, spec);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, holder);

        (await service.GetByIdAsync(seeded.Id)).Should().NotBeNull();
        (await service.GetByEntityAnalysisModelIdAsync(modelId)).Should().Contain(d => d.Id == seeded.Id);
        (await service.GetAsync()).Should().Contain(d => d.Id == seeded.Id);
    }

    [Fact]
    public async Task ARoleHoldingNoneOfTheThreeCannotReadTheRuleAsync()
    {
        var (modelId, seeded) = await SeedRuleAsync();
        var holder = await users.CreateAsync("None", 1);
        await using var dbContext = fx.GetDbContext();
        var service = await BuildServiceAsync(dbContext, holder);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seeded.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByEntityAnalysisModelIdAsync(modelId));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
    }

    [Fact]
    public void TheServiceDeclaresTheEditPermissionAsTheOriginalNumberAndReadsAsAnyOfTheThree()
    {
        EntityAnalysisModelReprocessingRuleService.EditPermissions.Should().Equal(EditRule);
        EntityAnalysisModelReprocessingRuleService.RequiredPermissions.Should()
            .BeEquivalentTo(new[] { EditRule, TriggerRun, StopRun });
    }
}

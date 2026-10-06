using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelActivationRule;
using Jube.Service.EntityAnalysisModelActivationRule;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.EntityAnalysisModelActivationRule
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelActivationRuleTtlCounterTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.GetTable<EntityAnalysisModelActivationRuleVersion>()
                    .Where(w => w.EntityAnalysisModelActivationRuleId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        [Fact]
        public async Task UpdateKeepsTtlGuidsWhenSwitchedOffAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext);
            var service = await EntityAnalysisModelActivationRuleService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus(), TestLog.NoOp);

            var modelGuid = Guid.NewGuid();
            var counterGuid = Guid.NewGuid();

            var dto = NewDto(modelId, "Probe" + Guid.NewGuid().ToString("N")[..8]);
            dto.EnableTtlCounter = true;
            dto.EntityAnalysisModelGuidTtlCounter = modelGuid;
            dto.EntityAnalysisModelTtlCounterGuid = counterGuid;

            var inserted = await service.InsertAsync(dto);
            createdIds.Add(inserted.Id);

            var roundTrip = await service.GetByIdAsync(inserted.Id);
            roundTrip.Should().NotBeNull();

            roundTrip!.EntityAnalysisModelGuidTtlCounter = modelGuid;
            roundTrip.EntityAnalysisModelTtlCounterGuid = counterGuid;
            roundTrip.Priority = 7;

            await service.UpdateAsync(roundTrip);

            var stored = await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>()
                .FirstAsync(w => w.Id == inserted.Id);

            stored.EnableTtlCounter.Should().Be(1);
            stored.EntityAnalysisModelGuidTtlCounter.Should().Be(modelGuid);
            stored.EntityAnalysisModelTtlCounterGuid.Should().Be(counterGuid);

            var versionRow = await dbContext.GetTable<EntityAnalysisModelActivationRuleVersion>()
                .FirstAsync(w => w.EntityAnalysisModelActivationRuleId == inserted.Id);

            versionRow.EntityAnalysisModelGuidTtlCounter.Should().Be(modelGuid);
            versionRow.EntityAnalysisModelTtlCounterGuid.Should().Be(counterGuid);

            var switchedOff = await service.GetByIdAsync(inserted.Id);
            switchedOff!.EnableTtlCounter = false;

            await service.UpdateAsync(switchedOff);

            var afterSwitchOff = await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>()
                .FirstAsync(w => w.Id == inserted.Id);

            afterSwitchOff.EnableTtlCounter.Should().Be(0);
            afterSwitchOff.EntityAnalysisModelGuidTtlCounter.Should().Be(modelGuid);
            afterSwitchOff.EntityAnalysisModelTtlCounterGuid.Should().Be(counterGuid);
        }

        private async Task<int> CreateParentModelAsync(DbContext dbContext)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission);
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

        private static EntityAnalysisModelActivationRuleDto NewDto(int entityAnalysisModelId, string name)
        {
            return new EntityAnalysisModelActivationRuleDto
            {
                EntityAnalysisModelId = entityAnalysisModelId,
                Name = name,
                BuilderRuleScript = "If (1 > 0) Then\n   Return True\nEnd If",
                Json = "{\"valid\":true,\"condition\":\"AND\",\"rules\":[]}",
                CoderRuleScript = "Return True",
                RuleScriptTypeId = 1,
                ActivationSample = 1,
                Priority = 0
            };
        }
    }
}
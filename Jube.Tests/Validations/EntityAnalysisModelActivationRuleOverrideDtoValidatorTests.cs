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
using Jube.Dto.EntityAnalysisModelActivationRuleOverride;
using Jube.Dto.Overrides;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Validations.EntityAnalysisModelActivationRuleOverride;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Validations
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelActivationRuleOverrideDtoValidatorTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizer Localiser =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                    NullLoggerFactory.Instance)
                .Create(typeof(EntityAnalysisModelActivationRuleOverrideDtoValidatorTests));

        private readonly List<int> createdActivationRuleIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdActivationRuleIds)
            {
                await dbContext.GetTable<EntityAnalysisModelActivationRule>().Where(w => w.Id == id)
                    .DeleteAsync().ConfigureAwait(false);
            }

            foreach (var id in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModel>().Where(w => w.Id == id)
                    .DeleteAsync().ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task ForceIsRefusedWhereTheActivationRuleIsNotEnabledForForceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, modelGuid) = await CreateModelAsync(dbContext);
            var ruleName = await CreateRuleAsync(dbContext, modelId, enableForce: 0);

            var result = await ValidateAsync(dbContext, Dto(modelGuid, "IP", ruleName,
                EntityAnalysisModelOverrideKind.Force));

            result.IsValid.Should().BeFalse(
                "a rule that may be muted is not thereby a rule that may be made to fire");
            result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(EntityAnalysisModelActivationRuleOverrideDto
                    .EntityAnalysisModelActivationRuleName));
        }

        [Fact]
        public async Task SuppressIsAdmittedWhereTheActivationRuleIsNotEnabledForForceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, modelGuid) = await CreateModelAsync(dbContext);
            var ruleName = await CreateRuleAsync(dbContext, modelId, enableForce: 0);

            var result = await ValidateAsync(dbContext, Dto(modelGuid, "IP", ruleName,
                EntityAnalysisModelOverrideKind.Suppress));

            result.IsValid.Should().BeTrue(
                "withdrawing Enable Force stops the rule being forced without stopping it being suppressed");
        }

        [Fact]
        public async Task ForceIsAdmittedWhereTheActivationRuleIsEnabledForForceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, modelGuid) = await CreateModelAsync(dbContext);
            var ruleName = await CreateRuleAsync(dbContext, modelId);

            var result = await ValidateAsync(dbContext, Dto(modelGuid, "IP", ruleName,
                EntityAnalysisModelOverrideKind.Force));

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task AnyKindIsRefusedWhereTheActivationRuleIsNotEnabledForOverrideAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, modelGuid) = await CreateModelAsync(dbContext);
            var ruleName = await CreateRuleAsync(dbContext, modelId, 0, 0);

            var result = await ValidateAsync(dbContext, Dto(modelGuid, "IP", ruleName,
                EntityAnalysisModelOverrideKind.Suppress));

            result.IsValid.Should().BeFalse(
                "a rule is not drawn into the override surface simply by existing");
        }

        [Fact]
        public async Task OverrideIsRefusedWhereTheActivationRuleIsBoundToAnotherKeyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, modelGuid) = await CreateModelAsync(dbContext);
            var ruleName = await CreateRuleAsync(dbContext, modelId, overrideKey: "AccountId");

            var mismatched = await ValidateAsync(dbContext, Dto(modelGuid, "IP", ruleName,
                EntityAnalysisModelOverrideKind.Suppress));

            mismatched.IsValid.Should().BeFalse(
                "a rule bound to one override key must not be reachable from another");

            var matched = await ValidateAsync(dbContext, Dto(modelGuid, "AccountId", ruleName,
                EntityAnalysisModelOverrideKind.Suppress));

            matched.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task OverrideIsAdmittedFromAnyKeyWhereTheActivationRuleBindsNoKeyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, modelGuid) = await CreateModelAsync(dbContext);
            var ruleName = await CreateRuleAsync(dbContext, modelId);

            var first = await ValidateAsync(dbContext, Dto(modelGuid, "IP", ruleName,
                EntityAnalysisModelOverrideKind.Suppress));
            var second = await ValidateAsync(dbContext, Dto(modelGuid, "AccountId", ruleName,
                EntityAnalysisModelOverrideKind.Suppress));

            first.IsValid.Should().BeTrue();
            second.IsValid.Should().BeTrue(
                "an empty Override Key leaves the rule reachable from every override enabled key");
        }

        private Task<FluentValidation.Results.ValidationResult> ValidateAsync(DbContext dbContext,
            EntityAnalysisModelActivationRuleOverrideDto dto)
        {
            var validator = new EntityAnalysisModelActivationRuleOverrideDtoValidator(
                new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission),
                new EntityAnalysisModelActivationRuleRepository(dbContext, fx.Seed.UserWithPermission),
                Localiser);

            return validator.ValidateAsync(dto);
        }

        private static EntityAnalysisModelActivationRuleOverrideDto Dto(Guid modelGuid, string overrideKey,
            string ruleName, EntityAnalysisModelOverrideKind overrideKind)
        {
            return new EntityAnalysisModelActivationRuleOverrideDto
            {
                EntityAnalysisModelGuid = modelGuid,
                OverrideKey = overrideKey,
                OverrideKeyValue = $"{DatabaseFixture.Prefix}Value{Guid.NewGuid():N}"[..40],
                EntityAnalysisModelActivationRuleName = ruleName,
                OverrideKind = overrideKind
            };
        }

        private async Task<(int Id, Guid Guid)> CreateModelAsync(DbContext dbContext)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission);
            var saved = await repository.InsertAsync(new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            }).ConfigureAwait(false);

            createdModelIds.Add(saved.Id);
            return (saved.Id, saved.Guid);
        }

        private async Task<string> CreateRuleAsync(DbContext dbContext, int entityAnalysisModelId,
            byte enableOverride = 1, byte enableForce = 1, string? overrideKey = null)
        {
            var repository = new EntityAnalysisModelActivationRuleRepository(dbContext,
                fx.Seed.UserWithPermission);
            var ruleName = $"{DatabaseFixture.Prefix}Rule{Guid.NewGuid():N}"[..40];
            var saved = await repository.InsertAsync(new EntityAnalysisModelActivationRule
            {
                EntityAnalysisModelId = entityAnalysisModelId,
                Name = ruleName,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableOverride = enableOverride,
                EnableForce = enableForce,
                OverrideKey = overrideKey
            }).ConfigureAwait(false);

            createdActivationRuleIds.Add(saved.Id);
            return ruleName;
        }
    }
}
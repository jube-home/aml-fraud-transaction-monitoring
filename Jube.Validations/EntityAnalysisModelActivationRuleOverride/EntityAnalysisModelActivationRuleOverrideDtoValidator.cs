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

using FluentValidation;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelActivationRuleOverride;
using Jube.Dto.Overrides;
using Jube.Resources;
using Jube.Validations.EntityAnalysisModelActivationRuleOverride.Models;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelActivationRuleOverride
{
    public sealed class EntityAnalysisModelActivationRuleOverrideDtoValidator
        : AbstractValidator<EntityAnalysisModelActivationRuleOverrideDto>
    {
        private const int MaxOverrideKeyLength = 256;
        private const int MaxOverrideKeyValueLength = 256;
        private const int MaxEntityAnalysisModelActivationRuleNameLength = 256;

        private readonly EntityAnalysisModelRepository models;
        private readonly EntityAnalysisModelActivationRuleRepository rules;

        public EntityAnalysisModelActivationRuleOverrideDtoValidator(
            EntityAnalysisModelRepository entityAnalysisModelRepository,
            EntityAnalysisModelActivationRuleRepository entityAnalysisModelActivationRuleRepository,
            IStringLocalizer localiser)
        {
            models = entityAnalysisModelRepository;
            rules = entityAnalysisModelActivationRuleRepository;

            RuleFor(p => p.OverrideKey)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleOverrideResources.OverrideKeyRequired])
                .WithErrorCode("OverrideKeyNotEmpty")
                .MaximumLength(MaxOverrideKeyLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[EntityAnalysisModelActivationRuleOverrideResources.OverrideKeyMaxLength],
                        MaxOverrideKeyLength))
                .WithErrorCode("OverrideKeyMaximumLength");

            RuleFor(p => p.OverrideKeyValue)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleOverrideResources.OverrideKeyValueRequired])
                .WithErrorCode("OverrideKeyValueNotEmpty")
                .MaximumLength(MaxOverrideKeyValueLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[
                            EntityAnalysisModelActivationRuleOverrideResources.OverrideKeyValueMaxLength],
                        MaxOverrideKeyValueLength))
                .WithErrorCode("OverrideKeyValueMaximumLength");

            RuleFor(p => p.EntityAnalysisModelGuid)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleOverrideResources.EntityAnalysisModelGuidRequired])
                .WithErrorCode("EntityAnalysisModelGuidNotEmpty")
                .MustAsync(async (guid, cancellation) =>
                    await entityAnalysisModelRepository.GetByGuidAsync(guid, cancellation) != null)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleOverrideResources.EntityAnalysisModelGuidNotFound])
                .WithErrorCode("EntityAnalysisModelGuidNotFound");

            RuleFor(p => p.EntityAnalysisModelActivationRuleName)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[
                        EntityAnalysisModelActivationRuleOverrideResources
                            .EntityAnalysisModelActivationRuleNameRequired])
                .WithErrorCode("EntityAnalysisModelActivationRuleNameNotEmpty")
                .MaximumLength(MaxEntityAnalysisModelActivationRuleNameLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[
                            EntityAnalysisModelActivationRuleOverrideResources
                                .EntityAnalysisModelActivationRuleNameMaxLength],
                        MaxEntityAnalysisModelActivationRuleNameLength))
                .WithErrorCode("EntityAnalysisModelActivationRuleNameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    if (dto.EntityAnalysisModelGuid == Guid.Empty)
                    {
                        return true;
                    }

                    var entityAnalysisModel = await entityAnalysisModelRepository
                        .GetByGuidAsync(dto.EntityAnalysisModelGuid, cancellation);
                    if (entityAnalysisModel == null)
                    {
                        return true;
                    }

                    var entityAnalysisModelActivationRule = await entityAnalysisModelActivationRuleRepository
                        .GetByNameEntityAnalysisModelIdAsync(name, entityAnalysisModel.Id, cancellation);
                    return entityAnalysisModelActivationRule != null;
                })
                .WithMessage(_ =>
                    localiser[
                        EntityAnalysisModelActivationRuleOverrideResources
                            .EntityAnalysisModelActivationRuleNameNotFound])
                .WithErrorCode("EntityAnalysisModelActivationRuleNameNotFound");

            RuleFor(p => p.EntityAnalysisModelActivationRuleName)
                .MustAsync(async (dto, name, cancellation) =>
                    await RuleAllowsAsync(dto, name, cancellation) != RuleAdmission.NotEnabledForOverride)
                .WithMessage(_ =>
                    localiser[
                        EntityAnalysisModelActivationRuleOverrideResources.ActivationRuleNotEnabledForOverride])
                .WithErrorCode("ActivationRuleNotEnabledForOverride")
                .MustAsync(async (dto, name, cancellation) =>
                    await RuleAllowsAsync(dto, name, cancellation) != RuleAdmission.NotEnabledForForce)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleOverrideResources.ActivationRuleNotEnabledForForce])
                .WithErrorCode("ActivationRuleNotEnabledForForce")
                .MustAsync(async (dto, name, cancellation) =>
                    await RuleAllowsAsync(dto, name, cancellation) != RuleAdmission.KeyMismatch)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleOverrideResources.ActivationRuleOverrideKeyMismatch])
                .WithErrorCode("ActivationRuleOverrideKeyMismatch");

            RuleFor(p => p.OverrideKind)
                .IsInEnum()
                .WithMessage(_ => localiser[EntityAnalysisModelActivationRuleOverrideResources.OverrideKindInvalid])
                .WithErrorCode("OverrideKindInvalid");

            RuleFor(p => p.DeleteExpiryDate)
                .GreaterThan(_ => DateTimeOffset.UtcNow)
                .When(p => p.DeleteExpiryDate.HasValue)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleOverrideResources.DeleteExpiryDateMustBeFuture])
                .WithErrorCode("DeleteExpiryDateMustBeFuture");
        }

        private async Task<RuleAdmission> RuleAllowsAsync(
            EntityAnalysisModelActivationRuleOverrideDto dto, string? name, CancellationToken cancellation)
        {
            if (dto.EntityAnalysisModelGuid == Guid.Empty || string.IsNullOrEmpty(name))
            {
                return RuleAdmission.Unknown;
            }

            var entityAnalysisModel = await models.GetByGuidAsync(dto.EntityAnalysisModelGuid, cancellation);

            if (entityAnalysisModel == null)
            {
                return RuleAdmission.Unknown;
            }

            var rule = await rules.GetByNameEntityAnalysisModelIdAsync(name, entityAnalysisModel.Id, cancellation);

            if (rule == null)
            {
                return RuleAdmission.Unknown;
            }

            if (rule.EnableOverride != 1)
            {
                return RuleAdmission.NotEnabledForOverride;
            }

            if (dto.OverrideKind == EntityAnalysisModelOverrideKind.Force && rule.EnableForce != 1)
            {
                return RuleAdmission.NotEnabledForForce;
            }

            if (!string.IsNullOrEmpty(rule.OverrideKey) && rule.OverrideKey != dto.OverrideKey)
            {
                return RuleAdmission.KeyMismatch;
            }

            return RuleAdmission.Admissible;
        }
    }
}
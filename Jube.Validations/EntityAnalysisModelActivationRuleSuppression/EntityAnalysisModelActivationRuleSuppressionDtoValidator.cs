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
using Jube.Dto.EntityAnalysisModelActivationRuleSuppression;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelActivationRuleSuppression
{
    public sealed class EntityAnalysisModelActivationRuleSuppressionDtoValidator
        : AbstractValidator<EntityAnalysisModelActivationRuleSuppressionDto>
    {
        private const int MaxSuppressionKeyLength = 256;
        private const int MaxSuppressionKeyValueLength = 256;
        private const int MaxEntityAnalysisModelActivationRuleNameLength = 256;

        public EntityAnalysisModelActivationRuleSuppressionDtoValidator(
            EntityAnalysisModelRepository entityAnalysisModelRepository,
            EntityAnalysisModelActivationRuleRepository entityAnalysisModelActivationRuleRepository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.SuppressionKey)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleSuppressionResources.SuppressionKeyRequired])
                .WithErrorCode("SuppressionKeyNotEmpty")
                .MaximumLength(MaxSuppressionKeyLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[EntityAnalysisModelActivationRuleSuppressionResources.SuppressionKeyMaxLength],
                        MaxSuppressionKeyLength))
                .WithErrorCode("SuppressionKeyMaximumLength");

            RuleFor(p => p.SuppressionKeyValue)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleSuppressionResources.SuppressionKeyValueRequired])
                .WithErrorCode("SuppressionKeyValueNotEmpty")
                .MaximumLength(MaxSuppressionKeyValueLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[
                            EntityAnalysisModelActivationRuleSuppressionResources.SuppressionKeyValueMaxLength],
                        MaxSuppressionKeyValueLength))
                .WithErrorCode("SuppressionKeyValueMaximumLength");

            RuleFor(p => p.EntityAnalysisModelGuid)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleSuppressionResources.EntityAnalysisModelGuidRequired])
                .WithErrorCode("EntityAnalysisModelGuidNotEmpty")
                .MustAsync(async (guid, cancellation) =>
                    await entityAnalysisModelRepository.GetByGuidAsync(guid, cancellation) != null)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleSuppressionResources.EntityAnalysisModelGuidNotFound])
                .WithErrorCode("EntityAnalysisModelGuidNotFound");

            RuleFor(p => p.EntityAnalysisModelActivationRuleName)
                .NotEmpty()
                .WithMessage(_ =>
                    localiser[
                        EntityAnalysisModelActivationRuleSuppressionResources
                            .EntityAnalysisModelActivationRuleNameRequired])
                .WithErrorCode("EntityAnalysisModelActivationRuleNameNotEmpty")
                .MaximumLength(MaxEntityAnalysisModelActivationRuleNameLength)
                .WithMessage(_ =>
                    string.Format(
                        localiser[
                            EntityAnalysisModelActivationRuleSuppressionResources
                                .EntityAnalysisModelActivationRuleNameMaxLength],
                        MaxEntityAnalysisModelActivationRuleNameLength))
                .WithErrorCode("EntityAnalysisModelActivationRuleNameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    if (dto.EntityAnalysisModelGuid == Guid.Empty) return true;

                    var entityAnalysisModel = await entityAnalysisModelRepository
                        .GetByGuidAsync(dto.EntityAnalysisModelGuid, cancellation);
                    if (entityAnalysisModel == null) return true;

                    var entityAnalysisModelActivationRule = await entityAnalysisModelActivationRuleRepository
                        .GetByNameEntityAnalysisModelIdAsync(name, entityAnalysisModel.Id, cancellation);
                    return entityAnalysisModelActivationRule != null;
                })
                .WithMessage(_ =>
                    localiser[
                        EntityAnalysisModelActivationRuleSuppressionResources
                            .EntityAnalysisModelActivationRuleNameNotFound])
                .WithErrorCode("EntityAnalysisModelActivationRuleNameNotFound");

            RuleFor(p => p.DeleteExpiryDate)
                .GreaterThan(_ => DateTimeOffset.UtcNow)
                .When(p => p.DeleteExpiryDate.HasValue)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelActivationRuleSuppressionResources.DeleteExpiryDateMustBeFuture])
                .WithErrorCode("DeleteExpiryDateMustBeFuture");
        }
    }
}
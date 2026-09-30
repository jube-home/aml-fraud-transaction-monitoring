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
using Jube.Dto.EntityAnalysisModelOverride;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelOverride
{
    public sealed class EntityAnalysisModelOverrideDtoValidator
        : AbstractValidator<EntityAnalysisModelOverrideDto>
    {
        private const int MaxOverrideKeyLength = 256;
        private const int MaxOverrideKeyValueLength = 256;

        public EntityAnalysisModelOverrideDtoValidator(EntityAnalysisModelRepository entityAnalysisModelRepository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.OverrideKey)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelOverrideResources.OverrideKeyRequired])
                .WithErrorCode("OverrideKeyNotEmpty")
                .MaximumLength(MaxOverrideKeyLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelOverrideResources.OverrideKeyMaxLength],
                        MaxOverrideKeyLength))
                .WithErrorCode("OverrideKeyMaximumLength");

            RuleFor(p => p.OverrideKeyValue)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelOverrideResources.OverrideKeyValueRequired])
                .WithErrorCode("OverrideKeyValueNotEmpty")
                .MaximumLength(MaxOverrideKeyValueLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelOverrideResources.OverrideKeyValueMaxLength],
                        MaxOverrideKeyValueLength))
                .WithErrorCode("OverrideKeyValueMaximumLength");

            RuleFor(p => p.EntityAnalysisModelGuid)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelOverrideResources.EntityAnalysisModelGuidRequired])
                .WithErrorCode("EntityAnalysisModelGuidNotEmpty")
                .MustAsync(async (guid, cancellation) =>
                    await entityAnalysisModelRepository.GetByGuidAsync(guid, cancellation) != null)
                .WithMessage(_ => localiser[EntityAnalysisModelOverrideResources.EntityAnalysisModelGuidNotFound])
                .WithErrorCode("EntityAnalysisModelGuidNotFound");

            RuleFor(p => p.OverrideKind)
                .IsInEnum()
                .WithMessage(_ => localiser[EntityAnalysisModelOverrideResources.OverrideKindInvalid])
                .WithErrorCode("OverrideKindInvalid");

            RuleFor(p => p.DeleteExpiryDate)
                .GreaterThan(_ => DateTimeOffset.UtcNow)
                .When(p => p.DeleteExpiryDate.HasValue)
                .WithMessage(_ => localiser[EntityAnalysisModelOverrideResources.DeleteExpiryDateMustBeFuture])
                .WithErrorCode("DeleteExpiryDateMustBeFuture");
        }
    }
}
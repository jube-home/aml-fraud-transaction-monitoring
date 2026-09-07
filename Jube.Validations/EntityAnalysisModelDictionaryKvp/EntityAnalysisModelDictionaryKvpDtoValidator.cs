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
using Jube.Dto.EntityAnalysisModelDictionaryKvp;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelDictionaryKvp
{
    public sealed class EntityAnalysisModelDictionaryKvpDtoValidator
        : AbstractValidator<EntityAnalysisModelDictionaryKvpDto>
    {
        private const int MaxKvpKeyLength = 256;

        public EntityAnalysisModelDictionaryKvpDtoValidator(EntityAnalysisModelDictionaryRepository parentRepository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelDictionaryId)
                .GreaterThan(0)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelDictionaryKvpResources.EntityAnalysisModelDictionaryIdInvalid])
                .WithErrorCode("EntityAnalysisModelDictionaryIdInvalid")
                .MustAsync(async (entityAnalysisModelDictionaryId, cancellation) =>
                    await parentRepository.GetByIdAsync(entityAnalysisModelDictionaryId, cancellation) != null)
                .WithMessage(_ =>
                    localiser[EntityAnalysisModelDictionaryKvpResources.EntityAnalysisModelDictionaryIdInvalid])
                .WithErrorCode("EntityAnalysisModelDictionaryIdNotFound");

            RuleFor(p => p.KvpKey)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelDictionaryKvpResources.KvpKeyRequired])
                .WithErrorCode("KvpKeyNotEmpty")
                .MaximumLength(MaxKvpKeyLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelDictionaryKvpResources.KvpKeyMaxLength],
                        MaxKvpKeyLength))
                .WithErrorCode("KvpKeyMaximumLength");

            RuleFor(p => p.KvpValue)
                .Must(v => v.HasValue)
                .WithMessage(_ => localiser[EntityAnalysisModelDictionaryKvpResources.KvpValueRequired])
                .WithErrorCode("KvpValueNotEmpty");

            RuleFor(p => p.DeleteExpiryDate)
                .GreaterThan(_ => DateTimeOffset.UtcNow)
                .When(p => p.DeleteExpiryDate.HasValue)
                .WithMessage(_ => localiser[EntityAnalysisModelDictionaryKvpResources.DeleteExpiryDateMustBeFuture])
                .WithErrorCode("DeleteExpiryDateMustBeFuture");
        }
    }
}
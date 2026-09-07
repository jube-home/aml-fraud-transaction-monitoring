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
using Jube.Dto.EntityAnalysisModelListValue;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelListValue
{
    public sealed class EntityAnalysisModelListValueDtoValidator : AbstractValidator<EntityAnalysisModelListValueDto>
    {
        private const int MaxListValueLength = 512;

        public EntityAnalysisModelListValueDtoValidator(EntityAnalysisModelListRepository parentRepository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelListId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelListValueResources.EntityAnalysisModelListIdInvalid])
                .WithErrorCode("EntityAnalysisModelListIdInvalid")
                .MustAsync(async (entityAnalysisModelListId, cancellation) =>
                    await parentRepository.GetByIdAsync(entityAnalysisModelListId, cancellation) != null)
                .WithMessage(_ => localiser[EntityAnalysisModelListValueResources.EntityAnalysisModelListIdInvalid])
                .WithErrorCode("EntityAnalysisModelListIdNotFound");

            RuleFor(p => p.ListValue)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelListValueResources.ListValueRequired])
                .WithErrorCode("ListValueNotEmpty")
                .MaximumLength(MaxListValueLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelListValueResources.ListValueMaxLength],
                        MaxListValueLength))
                .WithErrorCode("ListValueMaximumLength");

            RuleFor(p => p.DeleteExpiryDate)
                .GreaterThan(_ => DateTimeOffset.UtcNow)
                .When(p => p.DeleteExpiryDate.HasValue)
                .WithMessage(_ => localiser[EntityAnalysisModelListValueResources.DeleteExpiryDateMustBeFuture])
                .WithErrorCode("DeleteExpiryDateMustBeFuture");
        }
    }
}
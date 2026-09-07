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
using Jube.Dto.EntityAnalysisModelSuppression;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelSuppression
{
    public sealed class EntityAnalysisModelSuppressionDtoValidator
        : AbstractValidator<EntityAnalysisModelSuppressionDto>
    {
        private const int MaxSuppressionKeyLength = 256;
        private const int MaxSuppressionKeyValueLength = 256;

        public EntityAnalysisModelSuppressionDtoValidator(EntityAnalysisModelRepository entityAnalysisModelRepository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.SuppressionKey)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSuppressionResources.SuppressionKeyRequired])
                .WithErrorCode("SuppressionKeyNotEmpty")
                .MaximumLength(MaxSuppressionKeyLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelSuppressionResources.SuppressionKeyMaxLength],
                        MaxSuppressionKeyLength))
                .WithErrorCode("SuppressionKeyMaximumLength");

            RuleFor(p => p.SuppressionKeyValue)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSuppressionResources.SuppressionKeyValueRequired])
                .WithErrorCode("SuppressionKeyValueNotEmpty")
                .MaximumLength(MaxSuppressionKeyValueLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelSuppressionResources.SuppressionKeyValueMaxLength],
                        MaxSuppressionKeyValueLength))
                .WithErrorCode("SuppressionKeyValueMaximumLength");

            RuleFor(p => p.EntityAnalysisModelGuid)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSuppressionResources.EntityAnalysisModelGuidRequired])
                .WithErrorCode("EntityAnalysisModelGuidNotEmpty")
                .MustAsync(async (guid, cancellation) =>
                    await entityAnalysisModelRepository.GetByGuidAsync(guid, cancellation) != null)
                .WithMessage(_ => localiser[EntityAnalysisModelSuppressionResources.EntityAnalysisModelGuidNotFound])
                .WithErrorCode("EntityAnalysisModelGuidNotFound");

            RuleFor(p => p.DeleteExpiryDate)
                .GreaterThan(_ => DateTimeOffset.UtcNow)
                .When(p => p.DeleteExpiryDate.HasValue)
                .WithMessage(_ => localiser[EntityAnalysisModelSuppressionResources.DeleteExpiryDateMustBeFuture])
                .WithErrorCode("DeleteExpiryDateMustBeFuture");
        }
    }
}
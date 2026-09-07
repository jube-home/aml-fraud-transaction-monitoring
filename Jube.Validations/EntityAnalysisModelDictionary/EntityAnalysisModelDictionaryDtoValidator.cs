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
using Jube.Dto.EntityAnalysisModelDictionary;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelDictionary
{
    public sealed class EntityAnalysisModelDictionaryDtoValidator : AbstractValidator<EntityAnalysisModelDictionaryDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxDataNameLength = 256;

        public EntityAnalysisModelDictionaryDtoValidator(EntityAnalysisModelDictionaryRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelGuid)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelDictionaryResources.EntityAnalysisModelGuidInvalid])
                .WithErrorCode("EntityAnalysisModelGuidInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelDictionaryResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelDictionaryResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelGuidAsync(name, dto.EntityAnalysisModelGuid, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelDictionaryResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.DataName)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelDictionaryResources.DataNameRequired])
                .WithErrorCode("DataNameNotEmpty")
                .MaximumLength(MaxDataNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelDictionaryResources.DataNameMaxLength],
                        MaxDataNameLength))
                .WithErrorCode("DataNameMaximumLength");
        }
    }
}
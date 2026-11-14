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

using System.Text.RegularExpressions;
using FluentValidation;
using Jube.Data.Repository;
using Jube.Dto.Repository.EntityAnalysisModelSvirfd;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.EntityAnalysisModelSvirfd
{
    public sealed partial class EntityAnalysisModelSvirfdDtoValidator : AbstractValidator<EntityAnalysisModelSvirfdDto>
    {
        private const int MaxNameLength = 64;
        private const int MaxKeyLength = 128;

        public EntityAnalysisModelSvirfdDtoValidator(EntityAnalysisModelSvirfdRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .MustAsync(async (entityAnalysisModelId, cancellation) =>
                    await repository.ExistsEntityAnalysisModelAsync(entityAnalysisModelId, cancellation))
                .WithMessage(_ => localiser[EntityAnalysisModelSvirfdResources.EntityAnalysisModelNotFound])
                .WithErrorCode("EntityAnalysisModelNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSvirfdResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelSvirfdResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .Must(name => Identifier().IsMatch(name!))
                .WithMessage(_ => localiser[EntityAnalysisModelSvirfdResources.NameNotIdentifier])
                .WithErrorCode("NameNotIdentifier")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameEntityAnalysisModelIdAsync(name!, dto.EntityAnalysisModelId,
                        cancellation);

                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelSvirfdResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.RequestXPathName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSvirfdResources.RequestXPathNameRequired])
                .WithErrorCode("RequestXPathNameNotEmpty")
                .MaximumLength(MaxKeyLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelSvirfdResources.KeyMaxLength], MaxKeyLength))
                .WithErrorCode("RequestXPathNameMaximumLength")
                .MustAsync(async (dto, requestXPathName, cancellation) =>
                    await repository.IsIpRequestXPathAsync(requestXPathName!, dto.EntityAnalysisModelId, cancellation))
                .WithMessage(_ => localiser[EntityAnalysisModelSvirfdResources.RequestXPathNotIp])
                .WithErrorCode("RequestXPathNotIp");
        }

        [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*$")]
        private static partial Regex Identifier();
    }
}

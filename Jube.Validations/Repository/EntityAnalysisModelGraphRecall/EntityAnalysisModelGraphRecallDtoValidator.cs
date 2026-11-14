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
using Jube.Dto.Repository.EntityAnalysisModelGraphRecall;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.EntityAnalysisModelGraphRecall
{
    public sealed partial class EntityAnalysisModelGraphRecallDtoValidator : AbstractValidator<EntityAnalysisModelGraphRecallDto>
    {
        private const int MaxNameLength = 64;
        private const int MaxKeyLength = 128;

        public EntityAnalysisModelGraphRecallDtoValidator(EntityAnalysisModelGraphRecallRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .MustAsync(async (entityAnalysisModelId, cancellation) =>
                    await repository.ExistsEntityAnalysisModelAsync(entityAnalysisModelId, cancellation))
                .WithMessage(_ => localiser[EntityAnalysisModelGraphRecallResources.EntityAnalysisModelNotFound])
                .WithErrorCode("EntityAnalysisModelNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelGraphRecallResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelGraphRecallResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .Must(name => Identifier().IsMatch(name!))
                .WithMessage(_ => localiser[EntityAnalysisModelGraphRecallResources.NameNotIdentifier])
                .WithErrorCode("NameNotIdentifier")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameEntityAnalysisModelIdAsync(name!, dto.EntityAnalysisModelId,
                        cancellation);

                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelGraphRecallResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.RequestXPathName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelGraphRecallResources.RequestXPathNameRequired])
                .WithErrorCode("RequestXPathNameNotEmpty")
                .MaximumLength(MaxKeyLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelGraphRecallResources.KeyMaxLength], MaxKeyLength))
                .WithErrorCode("RequestXPathNameMaximumLength");

            RuleFor(p => p.NodeKey)
                .MaximumLength(MaxKeyLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelGraphRecallResources.KeyMaxLength], MaxKeyLength))
                .WithErrorCode("NodeKeyMaximumLength");

            RuleFor(p => p.ResponsePayload)
                .Must((dto, responsePayload) => !(responsePayload && dto.ReportTable))
                .WithMessage(_ => localiser[EntityAnalysisModelGraphRecallResources.ReportTableAndResponsePayloadExclusive])
                .WithErrorCode("ReportTableAndResponsePayloadExclusive");

            RuleFor(p => p.CounterpartName)
                .Cascade(CascadeMode.Stop)
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelGraphRecallResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("CounterpartNameMaximumLength")
                .Must((dto, counterpart) => !string.Equals(dto.Name, counterpart, StringComparison.OrdinalIgnoreCase))
                .When(p => !string.IsNullOrWhiteSpace(p.CounterpartName))
                .WithMessage(_ => localiser[EntityAnalysisModelGraphRecallResources.CounterpartIsSelf])
                .WithErrorCode("CounterpartIsSelf")
                .MustAsync(async (dto, counterpart, cancellation) =>
                    await repository.GetByNameEntityAnalysisModelIdAsync(counterpart!, dto.EntityAnalysisModelId,
                        cancellation) != null)
                .When(p => !string.IsNullOrWhiteSpace(p.CounterpartName))
                .WithMessage(_ => localiser[EntityAnalysisModelGraphRecallResources.CounterpartNotFound])
                .WithErrorCode("CounterpartNotFound");
        }

        [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*$")]
        private static partial Regex Identifier();
    }
}

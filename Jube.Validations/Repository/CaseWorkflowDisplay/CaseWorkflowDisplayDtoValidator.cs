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
using Jube.Dto.Repository.CaseWorkflowDisplay;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowDisplay
{
    public sealed class CaseWorkflowDisplayDtoValidator : AbstractValidator<CaseWorkflowDisplayDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxDescriptionLength = 1024;
        private const int MaxBlobLength = 1000000;

        public CaseWorkflowDisplayDtoValidator(CaseWorkflowDisplayRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseWorkflowDisplayResources.CaseWorkflowIdInvalid])
                .WithErrorCode("CaseWorkflowIdInvalid")
                .MustAsync(async (caseWorkflowId, cancellation) =>
                    await repository.ExistsCaseWorkflowAsync(caseWorkflowId, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowDisplayResources.CaseWorkflowNotFound])
                .WithErrorCode("CaseWorkflowIdNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowDisplayResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowDisplayResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameCaseWorkflowIdAsync(name, dto.CaseWorkflowId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[CaseWorkflowDisplayResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Description)
                .MaximumLength(MaxDescriptionLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowDisplayResources.DescriptionMaxLength],
                        MaxDescriptionLength))
                .WithErrorCode("DescriptionMaximumLength");

            RuleFor(p => p.Html)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowDisplayResources.HtmlRequired])
                .WithErrorCode("HtmlNotEmpty");

            RuleFor(p => p.Html)
                .MaximumLength(MaxBlobLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowDisplayResources.FieldMaxLength], "Html", MaxBlobLength))
                .WithErrorCode("HtmlMaximumLength");
        }
    }
}
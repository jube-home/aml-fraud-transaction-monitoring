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
using Jube.Dto.Repository.CaseWorkflowFilter;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowFilter
{
    public sealed class CaseWorkflowFilterDtoValidator : AbstractValidator<CaseWorkflowFilterDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxDescriptionLength = 1024;
        private const int MaxBlobLength = 1000000;

        private static bool BeJson(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }

            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(value,
                    new System.Text.Json.JsonDocumentOptions { MaxDepth = 64 });
                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }

        public CaseWorkflowFilterDtoValidator(CaseWorkflowFilterRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseWorkflowFilterResources.CaseWorkflowIdInvalid])
                .WithErrorCode("CaseWorkflowIdInvalid")
                .MustAsync(async (caseWorkflowId, cancellation) =>
                    await repository.ExistsCaseWorkflowAsync(caseWorkflowId, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowFilterResources.CaseWorkflowNotFound])
                .WithErrorCode("CaseWorkflowIdNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowFilterResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowFilterResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameCaseWorkflowIdAsync(name, dto.CaseWorkflowId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[CaseWorkflowFilterResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Description)
                .MaximumLength(MaxDescriptionLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowFilterResources.DescriptionMaxLength],
                        MaxDescriptionLength))
                .WithErrorCode("DescriptionMaximumLength");

            RuleFor(p => p.SelectJson)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowFilterResources.SelectJsonRequired])
                .WithErrorCode("SelectJsonNotEmpty");

            RuleFor(p => p.FilterJson)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowFilterResources.FilterJsonRequired])
                .WithErrorCode("FilterJsonNotEmpty");

            RuleFor(p => p.SelectJson)
                .MaximumLength(MaxBlobLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowFilterResources.FieldMaxLength], "SelectJson",
                    MaxBlobLength))
                .WithErrorCode("SelectJsonMaximumLength");

            RuleFor(p => p.FilterJson)
                .MaximumLength(MaxBlobLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowFilterResources.FieldMaxLength], "FilterJson",
                    MaxBlobLength))
                .WithErrorCode("FilterJsonMaximumLength");

            RuleFor(p => p.FilterTokens)
                .MaximumLength(MaxBlobLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowFilterResources.FieldMaxLength], "FilterTokens",
                    MaxBlobLength))
                .WithErrorCode("FilterTokensMaximumLength");

            RuleFor(p => p.SelectJson)
                .Must(BeJson)
                .WithMessage(_ => localiser[CaseWorkflowFilterResources.JsonInvalid])
                .WithErrorCode("SelectJsonJson");

            RuleFor(p => p.FilterJson)
                .Must(BeJson)
                .WithMessage(_ => localiser[CaseWorkflowFilterResources.JsonInvalid])
                .WithErrorCode("FilterJsonJson");

            RuleFor(p => p.FilterTokens)
                .Must(BeJson)
                .WithMessage(_ => localiser[CaseWorkflowFilterResources.JsonInvalid])
                .WithErrorCode("FilterTokensJson");
        }
    }
}
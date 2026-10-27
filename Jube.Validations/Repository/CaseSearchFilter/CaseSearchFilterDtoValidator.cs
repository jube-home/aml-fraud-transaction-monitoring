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
using Jube.Dto.Repository.CaseSearchFilter;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseSearchFilter
{
    public sealed class CaseSearchFilterDtoValidator : AbstractValidator<CaseSearchFilterDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxJsonLength = 1000000;

        private static bool BeJson(string? value)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(value!,
                    new System.Text.Json.JsonDocumentOptions { MaxDepth = 64 });

                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }

        public CaseSearchFilterDtoValidator(CaseSearchFilterRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseSearchFilterResources.CaseWorkflowGuidRequired])
                .WithErrorCode("CaseWorkflowGuidNotEmpty")
                .MustAsync(async (caseWorkflowGuid, cancellation) =>
                    await repository.GetAccessibleCaseWorkflowIdAsync(caseWorkflowGuid, cancellation) != null)
                .WithMessage(_ => localiser[CaseSearchFilterResources.CaseWorkflowNotFound])
                .WithErrorCode("CaseWorkflowGuidNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseSearchFilterResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[CaseSearchFilterResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var caseWorkflowId = await repository
                        .GetAccessibleCaseWorkflowIdAsync(dto.CaseWorkflowGuid, cancellation);

                    if (caseWorkflowId == null)
                    {
                        return true;
                    }

                    var existing = await repository.GetOwnedByNameAsync(name!.Trim(), caseWorkflowId.Value,
                        cancellation);

                    return existing == null || existing.Guid == dto.Guid;
                })
                .WithMessage(_ => localiser[CaseSearchFilterResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.FilterJson)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseSearchFilterResources.FilterJsonRequired])
                .WithErrorCode("FilterJsonNotEmpty")
                .MaximumLength(MaxJsonLength)
                .WithMessage(_ => string.Format(localiser[CaseSearchFilterResources.JsonTooLarge], "Filter Json"))
                .WithErrorCode("FilterJsonMaximumLength")
                .Must(BeJson)
                .WithMessage(_ => string.Format(localiser[CaseSearchFilterResources.JsonInvalid], "Filter Json"))
                .WithErrorCode("FilterJsonInvalid");

            RuleFor(p => p.SelectJson)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseSearchFilterResources.SelectJsonRequired])
                .WithErrorCode("SelectJsonNotEmpty")
                .MaximumLength(MaxJsonLength)
                .WithMessage(_ => string.Format(localiser[CaseSearchFilterResources.JsonTooLarge], "Select Json"))
                .WithErrorCode("SelectJsonMaximumLength")
                .Must(BeJson)
                .WithMessage(_ => string.Format(localiser[CaseSearchFilterResources.JsonInvalid], "Select Json"))
                .WithErrorCode("SelectJsonInvalid");

            RuleFor(p => p.SharedRoleRegistryGuid)
                .MustAsync(async (roleGuid, cancellation) =>
                    await repository.RoleExistsAsync(roleGuid!.Value, cancellation))
                .When(p => p.SharedRoleRegistryGuid.HasValue)
                .WithMessage(_ => localiser[CaseSearchFilterResources.SharedRoleNotFound])
                .WithErrorCode("SharedRoleRegistryGuidNotFound");
        }
    }
}

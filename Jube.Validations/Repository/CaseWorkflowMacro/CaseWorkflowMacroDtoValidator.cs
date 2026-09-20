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
using Jube.Dto.Repository.CaseWorkflowMacro;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowMacro
{
    public sealed class CaseWorkflowMacroDtoValidator : AbstractValidator<CaseWorkflowMacroDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxDescriptionLength = 1024;
        private const string ImageLocationPattern = @"^(?!.*\.\.)[A-Za-z0-9_.\- ]{0,256}$";
        private static readonly int[] httpEndpointTypeIds = [1, 2];
        private static readonly int[] notificationTypeIds = [1, 2];

        public CaseWorkflowMacroDtoValidator(CaseWorkflowMacroRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.CaseWorkflowIdInvalid])
                .WithErrorCode("CaseWorkflowIdInvalid")
                .MustAsync(async (caseWorkflowId, cancellation) =>
                    await repository.ExistsCaseWorkflowAsync(caseWorkflowId, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.CaseWorkflowNotFound])
                .WithErrorCode("CaseWorkflowIdNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowMacroResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameCaseWorkflowIdAsync(name, dto.CaseWorkflowId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Description)
                .MaximumLength(MaxDescriptionLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowMacroResources.DescriptionMaxLength], MaxDescriptionLength))
                .WithErrorCode("DescriptionMaximumLength");

            RuleFor(p => p.Javascript)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.JavascriptRequired])
                .WithErrorCode("JavascriptNotEmpty");

            RuleFor(p => p.HttpEndpointTypeId)
                .InclusiveBetween(0, byte.MaxValue)
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.HttpEndpointTypeIdInvalid])
                .WithErrorCode("HttpEndpointTypeIdInvalid");

            RuleFor(p => p.HttpEndpointTypeId)
                .Must(m => httpEndpointTypeIds.Contains(m))
                .When(w => w.EnableHttpEndpoint)
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.HttpEndpointTypeIdInvalid])
                .WithErrorCode("HttpEndpointTypeIdInvalid");

            RuleFor(p => p.HttpEndpoint)
                .NotEmpty()
                .When(w => w.EnableHttpEndpoint)
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.HttpEndpointRequired])
                .WithErrorCode("HttpEndpointNotEmpty");

            RuleFor(p => p.NotificationTypeId)
                .InclusiveBetween(0, byte.MaxValue)
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.NotificationTypeIdInvalid])
                .WithErrorCode("NotificationTypeIdInvalid");

            RuleFor(p => p.NotificationTypeId)
                .Must(m => notificationTypeIds.Contains(m))
                .When(w => w.EnableNotification)
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.NotificationTypeIdInvalid])
                .WithErrorCode("NotificationTypeIdInvalid");

            RuleFor(p => p.NotificationDestination)
                .NotEmpty()
                .When(w => w.EnableNotification)
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.NotificationDestinationRequired])
                .WithErrorCode("NotificationDestinationNotEmpty");
            RuleFor(p => p.ImageLocation)
                .Matches(ImageLocationPattern)
                .When(w => !string.IsNullOrEmpty(w.ImageLocation))
                .WithMessage(_ => localiser[CaseWorkflowMacroResources.ImageLocationInvalid])
                .WithErrorCode("ImageLocationInvalid");
        }
    }
}
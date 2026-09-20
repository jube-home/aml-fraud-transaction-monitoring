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
using Jube.Dto.Repository.CaseWorkflowStatus;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowStatus
{
    public sealed class CaseWorkflowStatusDtoValidator : AbstractValidator<CaseWorkflowStatusDto>
    {
        private const int MaxNameLength = 256;
        private static readonly int[] priorityValues = [1, 2, 3, 4, 5];
        private static readonly int[] httpEndpointTypeIds = [1, 2];
        private static readonly int[] notificationTypeIds = [1, 2];

        public CaseWorkflowStatusDtoValidator(CaseWorkflowStatusRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.CaseWorkflowIdInvalid])
                .WithErrorCode("CaseWorkflowIdInvalid")
                .MustAsync(async (caseWorkflowId, cancellation) =>
                    await repository.ExistsCaseWorkflowAsync(caseWorkflowId, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.CaseWorkflowNotFound])
                .WithErrorCode("CaseWorkflowIdNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowStatusResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameCaseWorkflowIdAsync(name, dto.CaseWorkflowId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Priority)
                .Must(m => priorityValues.Contains(m))
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.PriorityInvalid])
                .WithErrorCode("PriorityInvalid");

            RuleFor(p => p.ForeColor)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Matches("^#[0-9A-Fa-f]{6}$")
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.ForeColorRequired])
                .WithErrorCode("ForeColorFormat");

            RuleFor(p => p.BackColor)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Matches("^#[0-9A-Fa-f]{6}$")
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.BackColorRequired])
                .WithErrorCode("BackColorFormat");

            RuleFor(p => p.HttpEndpointTypeId)
                .InclusiveBetween(0, byte.MaxValue)
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.HttpEndpointTypeIdInvalid])
                .WithErrorCode("HttpEndpointTypeIdInvalid");

            RuleFor(p => p.HttpEndpointTypeId)
                .Must(m => httpEndpointTypeIds.Contains(m))
                .When(w => w.EnableHttpEndpoint)
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.HttpEndpointTypeIdInvalid])
                .WithErrorCode("HttpEndpointTypeIdInvalid");

            RuleFor(p => p.HttpEndpoint)
                .NotEmpty()
                .When(w => w.EnableHttpEndpoint)
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.HttpEndpointRequired])
                .WithErrorCode("HttpEndpointNotEmpty");

            RuleFor(p => p.NotificationTypeId)
                .InclusiveBetween(0, byte.MaxValue)
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.NotificationTypeIdInvalid])
                .WithErrorCode("NotificationTypeIdInvalid");

            RuleFor(p => p.NotificationTypeId)
                .Must(m => notificationTypeIds.Contains(m))
                .When(w => w.EnableNotification)
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.NotificationTypeIdInvalid])
                .WithErrorCode("NotificationTypeIdInvalid");

            RuleFor(p => p.NotificationDestination)
                .NotEmpty()
                .When(w => w.EnableNotification)
                .WithMessage(_ => localiser[CaseWorkflowStatusResources.NotificationDestinationRequired])
                .WithErrorCode("NotificationDestinationNotEmpty");
        }
    }
}
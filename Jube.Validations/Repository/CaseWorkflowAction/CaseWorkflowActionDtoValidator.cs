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
using Jube.Dto.Repository.CaseWorkflowAction;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowAction
{
    public sealed class CaseWorkflowActionDtoValidator : AbstractValidator<CaseWorkflowActionDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxDescriptionLength = 1024;
        private const int MaxEndpointLength = 2048;
        private const int MaxNotificationBodyLength = 100000;
        private const int MaxNotificationFieldLength = 1024;
        private static readonly int[] httpEndpointTypeIds = [1, 2];
        private static readonly int[] notificationTypeIds = [1, 2];

        public CaseWorkflowActionDtoValidator(CaseWorkflowActionRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseWorkflowActionResources.CaseWorkflowIdInvalid])
                .WithErrorCode("CaseWorkflowIdInvalid")
                .MustAsync(async (caseWorkflowId, cancellation) =>
                    await repository.ExistsCaseWorkflowAsync(caseWorkflowId, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowActionResources.CaseWorkflowNotFound])
                .WithErrorCode("CaseWorkflowIdNotFound");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowActionResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowActionResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameCaseWorkflowIdAsync(name, dto.CaseWorkflowId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[CaseWorkflowActionResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Description)
                .MaximumLength(MaxDescriptionLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowActionResources.DescriptionMaxLength], MaxDescriptionLength))
                .WithErrorCode("DescriptionMaximumLength");

            RuleFor(p => p.HttpEndpointTypeId)
                .Must(m => httpEndpointTypeIds.Contains(m))
                .When(w => w.EnableHttpEndpoint)
                .WithMessage(_ => localiser[CaseWorkflowActionResources.HttpEndpointTypeIdInvalid])
                .WithErrorCode("HttpEndpointTypeIdInvalid");

            RuleFor(p => p.HttpEndpoint)
                .NotEmpty()
                .When(w => w.EnableHttpEndpoint)
                .WithMessage(_ => localiser[CaseWorkflowActionResources.HttpEndpointRequired])
                .WithErrorCode("HttpEndpointNotEmpty");

            RuleFor(p => p.NotificationTypeId)
                .Must(m => notificationTypeIds.Contains(m))
                .When(w => w.EnableNotification)
                .WithMessage(_ => localiser[CaseWorkflowActionResources.NotificationTypeIdInvalid])
                .WithErrorCode("NotificationTypeIdInvalid");

            RuleFor(p => p.NotificationDestination)
                .NotEmpty()
                .When(w => w.EnableNotification)
                .WithMessage(_ => localiser[CaseWorkflowActionResources.NotificationDestinationRequired])
                .WithErrorCode("NotificationDestinationNotEmpty");

            RuleFor(p => p.HttpEndpoint)
                .MaximumLength(MaxEndpointLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowActionResources.FieldMaxLength], "HttpEndpoint",
                    MaxEndpointLength))
                .WithErrorCode("HttpEndpointMaximumLength");

            RuleFor(p => p.NotificationDestination)
                .MaximumLength(MaxNotificationFieldLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowActionResources.FieldMaxLength],
                    "NotificationDestination", MaxNotificationFieldLength))
                .WithErrorCode("NotificationDestinationMaximumLength");

            RuleFor(p => p.NotificationSubject)
                .MaximumLength(MaxNotificationFieldLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowActionResources.FieldMaxLength],
                    "NotificationSubject", MaxNotificationFieldLength))
                .WithErrorCode("NotificationSubjectMaximumLength");

            RuleFor(p => p.NotificationBody)
                .MaximumLength(MaxNotificationBodyLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowActionResources.FieldMaxLength],
                    "NotificationBody", MaxNotificationBodyLength))
                .WithErrorCode("NotificationBodyMaximumLength");
        }
    }
}
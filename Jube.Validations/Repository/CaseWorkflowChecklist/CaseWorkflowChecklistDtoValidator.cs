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
using Jube.Dto.Repository.CaseWorkflowChecklist;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowChecklist
{
    public sealed class CaseWorkflowChecklistDtoValidator : AbstractValidator<CaseWorkflowChecklistDto>
    {
        private const int MaxNameLength = 100;

        public CaseWorkflowChecklistDtoValidator(CaseWorkflowChecklistRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowStatusGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowChecklistResources.CaseWorkflowStatusGuidRequired])
                .WithErrorCode("CaseWorkflowStatusGuidNotEmpty")
                .MustAsync(async (guid, cancellation) =>
                    await repository.ExistsCaseWorkflowStatusAsync(guid, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowChecklistResources.CaseWorkflowStatusNotFound])
                .WithErrorCode("CaseWorkflowStatusGuidNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowChecklistResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => localiser[CaseWorkflowChecklistResources.NameTooLong])
                .WithErrorCode("NameMaximumLength");

            RuleFor(p => p.Priority)
                .GreaterThanOrEqualTo(0)
                .When(p => p.Priority.HasValue)
                .WithMessage(_ => localiser[CaseWorkflowChecklistResources.PriorityNegative])
                .WithErrorCode("PriorityGreaterThanOrEqualTo");
        }
    }
}

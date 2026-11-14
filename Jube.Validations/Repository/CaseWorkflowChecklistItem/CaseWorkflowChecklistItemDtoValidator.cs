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
using Jube.Dto.Repository.CaseWorkflowChecklistItem;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowChecklistItem
{
    public sealed class CaseWorkflowChecklistItemDtoValidator : AbstractValidator<CaseWorkflowChecklistItemDto>
    {
        private const int MaxNameLength = 100;
        private const int MaxMessageLength = 500;
        private const int MaxSqlLength = 8000;

        public CaseWorkflowChecklistItemDtoValidator(CaseWorkflowChecklistItemRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowChecklistGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.CaseWorkflowChecklistGuidRequired])
                .WithErrorCode("CaseWorkflowChecklistGuidNotEmpty")
                .MustAsync(async (guid, cancellation) =>
                    await repository.ExistsCaseWorkflowChecklistAsync(guid, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.CaseWorkflowChecklistNotFound])
                .WithErrorCode("CaseWorkflowChecklistGuidNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.NameTooLong])
                .WithErrorCode("NameMaximumLength");

            RuleFor(p => p.Priority)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.PriorityInvalid])
                .WithErrorCode("PriorityInvalid");

            When(p => p.SlaMinutes.HasValue, () =>
            {
                RuleFor(p => p.SlaMinutes)
                    .Must(minutes => minutes > 0)
                    .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.SlaMinutesInvalid])
                    .WithErrorCode("SlaMinutesInvalid");
            });

            RuleFor(p => p.ValidateMessage)
                .MaximumLength(MaxMessageLength)
                .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.ValidateMessageTooLong])
                .WithErrorCode("ValidateMessageMaximumLength");

            When(p => p.EnableShow, () =>
            {
                RuleFor(p => p.ShowSql)
                    .Must(sql => !string.IsNullOrWhiteSpace(sql))
                    .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.ShowSqlRequired])
                    .WithErrorCode("ShowSqlRequired");
            });

            When(p => p.EnableValidation, () =>
            {
                RuleFor(p => p.ValidateSql)
                    .Must(sql => !string.IsNullOrWhiteSpace(sql))
                    .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.ValidateSqlRequired])
                    .WithErrorCode("ValidateSqlRequired");
            });

            RuleFor(p => p.ShowSql)
                .MaximumLength(MaxSqlLength)
                .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.SqlTooLong])
                .WithErrorCode("ShowSqlMaximumLength");

            RuleFor(p => p.ValidateSql)
                .MaximumLength(MaxSqlLength)
                .WithMessage(_ => localiser[CaseWorkflowChecklistItemResources.SqlTooLong])
                .WithErrorCode("ValidateSqlMaximumLength");
        }
    }
}

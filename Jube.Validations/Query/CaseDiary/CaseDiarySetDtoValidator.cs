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
using Jube.Dto.Options;
using Jube.Dto.Query.CaseDiary;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Query.CaseDiary
{
    public sealed class CaseDiarySetDtoValidator : AbstractValidator<CaseDiarySetDto>
    {
        public const int PastYears = 1;
        public const int FutureYears = 10;

        public CaseDiarySetDtoValidator(IStringLocalizer localiser, DateTime now)
        {
            RuleFor(p => p.CaseId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseDiaryResources.CaseIdInvalid])
                .WithErrorCode("CaseIdInvalid");

            RuleFor(p => p.DiaryTypeId)
                .Must(m => Enum.IsDefined((CaseDiaryType)m))
                .WithMessage(_ => localiser[CaseDiaryResources.DiaryTypeInvalid])
                .WithErrorCode("DiaryTypeInvalid");

            RuleFor(p => p.DueDate)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage(_ => localiser[CaseDiaryResources.DueDateRequired])
                .WithErrorCode("DueDateRequired")
                .Must(m => m!.Value.UtcDateTime >= now.AddYears(-PastYears)
                           && m.Value.UtcDateTime <= now.AddYears(FutureYears))
                .WithMessage(_ => localiser[CaseDiaryResources.DueDateOutOfRange])
                .WithErrorCode("DueDateOutOfRange");
        }
    }
}

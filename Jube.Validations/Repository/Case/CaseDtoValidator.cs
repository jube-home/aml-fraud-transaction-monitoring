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
using Jube.Dto.Repository.Case;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.Case
{
    public sealed class CaseDtoValidator : AbstractValidator<CaseDto>
    {
        public CaseDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.Id)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseResources.IdRequired])
                .WithErrorCode("IdGreaterThan");

            RuleFor(p => p.ClosedStatusId)
                .GreaterThanOrEqualTo((byte)0)
                .WithErrorCode("ClosedStatusIdGreaterThanOrEqualTo");

            RuleFor(p => p.ClosedStatusId)
                .InclusiveBetween((byte)0, (byte)4)
                .WithErrorCode("ClosedStatusIdInclusiveBetween");

            RuleFor(p => p.LockedUser)
                .NotEmpty()
                .When(w => w.Locked)
                .WithMessage(_ => localiser[CaseResources.LockedUserRequired])
                .WithErrorCode("LockedUserNotEmpty");

            RuleFor(p => p.CaseWorkflowStatusGuid)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseResources.CaseWorkflowStatusGuidRequired])
                .WithErrorCode("CaseWorkflowStatusGuidNotEmpty");

            RuleFor(p => p.DiaryDate)
                .NotEmpty()
                .When(w => w.Diary)
                .WithMessage(_ => localiser[CaseResources.DiaryDateRequired])
                .WithErrorCode("DiaryDateNotEmpty");

            RuleFor(p => p.Rating)
                .InclusiveBetween((byte)1, (byte)5)
                .WithMessage(_ => localiser[CaseResources.RatingRequired])
                .WithErrorCode("RatingInclusiveBetween");

            RuleFor(p => p.Payload)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseResources.PayloadRequired])
                .WithErrorCode("PayloadNotEmpty");
        }
    }
}
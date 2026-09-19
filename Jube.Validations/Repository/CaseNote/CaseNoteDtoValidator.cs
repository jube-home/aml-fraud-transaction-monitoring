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
using Jube.Dto.Repository.CaseNote;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseNote
{
    public sealed class CaseNoteDtoValidator : AbstractValidator<CaseNoteDto>
    {
        private const int MaxNoteLength = 100000;

        public CaseNoteDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.Note)
                .MaximumLength(MaxNoteLength)
                .WithMessage(_ => string.Format(localiser[CaseNoteResources.NoteMaxLength], MaxNoteLength))
                .WithErrorCode("NoteMaximumLength");

            RuleFor(p => p.Note)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseNoteResources.NoteRequired])
                .WithErrorCode("NoteNotEmpty");

            RuleFor(p => p.ActionId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseNoteResources.ActionIdRequired])
                .WithErrorCode("ActionIdGreaterThan");

            RuleFor(p => p.PriorityId)
                .InclusiveBetween(1, 3)
                .WithMessage(_ => localiser[CaseNoteResources.PriorityIdRequired])
                .WithErrorCode("PriorityIdGreaterThan");

            RuleFor(p => p.CaseKey)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseNoteResources.CaseKeyRequired])
                .WithErrorCode("CaseKeyNotEmpty");

            RuleFor(p => p.CaseKeyValue)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseNoteResources.CaseKeyValueRequired])
                .WithErrorCode("CaseKeyValueNotEmpty");

            RuleFor(p => p.CaseId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseNoteResources.CaseIdRequired])
                .WithErrorCode("CaseIdGreaterThan");

            RuleFor(p => p.Payload)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseNoteResources.PayloadRequired])
                .WithErrorCode("PayloadNotEmpty");
        }
    }
}
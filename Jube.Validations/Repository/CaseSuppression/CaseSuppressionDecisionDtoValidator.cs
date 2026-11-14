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
using Jube.Dto.Repository.CaseSuppression;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseSuppression
{
    public sealed class CaseSuppressionDecisionDtoValidator : AbstractValidator<CaseSuppressionDecisionDto>
    {
        public const int MaxNoteLength = 1000;

        public CaseSuppressionDecisionDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.Id)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseSuppressionResources.IdRequired])
                .WithErrorCode("IdGreaterThan");

            RuleFor(p => p.Note)
                .MaximumLength(MaxNoteLength)
                .WithMessage(_ => string.Format(localiser[CaseSuppressionResources.NoteMaxLength], MaxNoteLength))
                .WithErrorCode("NoteMaximumLength");
        }
    }
}

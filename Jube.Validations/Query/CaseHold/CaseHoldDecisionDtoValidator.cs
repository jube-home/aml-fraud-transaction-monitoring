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
using Jube.Dto.Query.CaseHold;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Query.CaseHold
{
    public sealed class CaseHoldDecisionDtoValidator : AbstractValidator<CaseHoldDecisionDto>
    {
        private const int MaxReasonLength = 1000;

        public CaseHoldDecisionDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseHoldResources.CaseIdInvalid])
                .WithErrorCode("CaseIdInvalid");

            RuleFor(p => p.EntityAnalysisModelInstanceEntryGuid)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseHoldResources.EntryRequired])
                .WithErrorCode("EntityAnalysisModelInstanceEntryGuidNotEmpty");

            RuleFor(p => p.Reason)
                .Cascade(CascadeMode.Stop)
                .Must(m => !string.IsNullOrWhiteSpace(m))
                .WithMessage(_ => localiser[CaseHoldResources.ReasonRequired])
                .WithErrorCode("ReasonNotEmpty")
                .MaximumLength(MaxReasonLength)
                .WithMessage(_ => localiser[CaseHoldResources.ReasonTooLong])
                .WithErrorCode("ReasonMaximumLength");
        }
    }
}

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
using Jube.Dto.Repository.CaseWorkflowFormEntry;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowFormEntry
{
    public sealed class CaseWorkflowFormEntryDtoValidator : AbstractValidator<CaseWorkflowFormEntryDto>
    {
        private const int MaxPayloadFields = 500;
        private const int MaxFieldNameLength = 256;
        private const int MaxFieldValueLength = 1_000_000;

        public CaseWorkflowFormEntryDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowFormId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseWorkflowFormEntryResources.CaseWorkflowFormIdRequired])
                .WithErrorCode("CaseWorkflowFormIdGreaterThan");

            RuleFor(p => p.CaseKey)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowFormEntryResources.CaseKeyRequired])
                .WithErrorCode("CaseKeyNotEmpty");

            RuleFor(p => p.CaseKeyValue)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowFormEntryResources.CaseKeyValueRequired])
                .WithErrorCode("CaseKeyValueNotEmpty");

            RuleFor(p => p.CaseId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseWorkflowFormEntryResources.CaseIdRequired])
                .WithErrorCode("CaseIdGreaterThan");

            RuleFor(p => p.Payload)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowFormEntryResources.PayloadRequired])
                .WithErrorCode("PayloadNotEmpty")
                .Must(payload => payload is not null
                                 && payload.Count <= MaxPayloadFields
                                 && payload.Keys.All(k => k.Length <= MaxFieldNameLength)
                                 && payload.Values.All(v => v == null
                                                            || (v.ToString()?.Length ?? 0) <= MaxFieldValueLength))
                .WithMessage(_ => string.Format(localiser[CaseWorkflowFormEntryResources.PayloadTooLarge],
                    MaxPayloadFields, MaxFieldNameLength, MaxFieldValueLength))
                .WithErrorCode("PayloadTooLarge");
        }
    }
}
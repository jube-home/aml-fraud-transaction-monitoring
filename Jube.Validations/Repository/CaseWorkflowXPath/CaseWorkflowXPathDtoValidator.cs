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
using Jube.Dto.Repository.CaseWorkflowXPath;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowXPath
{
    public sealed class CaseWorkflowXPathDtoValidator : AbstractValidator<CaseWorkflowXPathDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxXPathLength = 1024;
        private const int MaxRegularExpressionLength = 1024;
        private const int MaxColorLength = 32;
        private const string ColorPattern = @"^(#[0-9A-Fa-f]{3,8}|[A-Za-z]{3,30}|rgba?\([0-9., %]{5,28}\))$";

        public CaseWorkflowXPathDtoValidator(CaseWorkflowXPathRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.CaseWorkflowIdInvalid])
                .WithErrorCode("CaseWorkflowIdInvalid")
                .MustAsync(async (caseWorkflowId, cancellation) =>
                    await repository.ExistsCaseWorkflowAsync(caseWorkflowId, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.CaseWorkflowNotFound])
                .WithErrorCode("CaseWorkflowIdNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowXPathResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameCaseWorkflowIdAsync(name, dto.CaseWorkflowId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.XPath)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.XPathRequired])
                .WithErrorCode("XPathNotEmpty")
                .MaximumLength(MaxXPathLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowXPathResources.XPathMaxLength], MaxXPathLength))
                .WithErrorCode("XPathMaximumLength");

            RuleFor(p => p.BoldLineFormatForeColor)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.BoldLineFormatForeColorRequired])
                .WithErrorCode("BoldLineFormatForeColorNotEmpty")
                .MaximumLength(MaxColorLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowXPathResources.ColorMaxLength], MaxColorLength))
                .WithErrorCode("BoldLineFormatForeColorMaximumLength")
                .When(w => w.BoldLineMatched);

            RuleFor(p => p.BoldLineFormatBackColor)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.BoldLineFormatBackColorRequired])
                .WithErrorCode("BoldLineFormatBackColorNotEmpty")
                .MaximumLength(MaxColorLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowXPathResources.ColorMaxLength], MaxColorLength))
                .WithErrorCode("BoldLineFormatBackColorMaximumLength")
                .When(w => w.BoldLineMatched);

            RuleFor(p => p.RegularExpression)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.RegularExpressionRequired])
                .WithErrorCode("RegularExpressionNotEmpty")
                .MaximumLength(MaxRegularExpressionLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowXPathResources.RegularExpressionMaxLength],
                    MaxRegularExpressionLength))
                .WithErrorCode("RegularExpressionMaximumLength")
                .When(w => w.ConditionalRegularExpressionFormatting);

            RuleFor(p => p.ConditionalFormatForeColor)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.ConditionalFormatForeColorRequired])
                .WithErrorCode("ConditionalFormatForeColorNotEmpty")
                .MaximumLength(MaxColorLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowXPathResources.ColorMaxLength], MaxColorLength))
                .WithErrorCode("ConditionalFormatForeColorMaximumLength")
                .When(w => w.ConditionalRegularExpressionFormatting);

            RuleFor(p => p.ConditionalFormatBackColor)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.ConditionalFormatBackColorRequired])
                .WithErrorCode("ConditionalFormatBackColorNotEmpty")
                .MaximumLength(MaxColorLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowXPathResources.ColorMaxLength], MaxColorLength))
                .WithErrorCode("ConditionalFormatBackColorMaximumLength")
                .When(w => w.ConditionalRegularExpressionFormatting);
            RuleFor(p => p.BoldLineFormatForeColor)
                .Matches(ColorPattern)
                .When(w => !string.IsNullOrEmpty(w.BoldLineFormatForeColor))
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.ColorInvalid])
                .WithErrorCode("BoldLineFormatForeColorInvalid");

            RuleFor(p => p.BoldLineFormatBackColor)
                .Matches(ColorPattern)
                .When(w => !string.IsNullOrEmpty(w.BoldLineFormatBackColor))
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.ColorInvalid])
                .WithErrorCode("BoldLineFormatBackColorInvalid");

            RuleFor(p => p.ConditionalFormatForeColor)
                .Matches(ColorPattern)
                .When(w => !string.IsNullOrEmpty(w.ConditionalFormatForeColor))
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.ColorInvalid])
                .WithErrorCode("ConditionalFormatForeColorInvalid");

            RuleFor(p => p.ConditionalFormatBackColor)
                .Matches(ColorPattern)
                .When(w => !string.IsNullOrEmpty(w.ConditionalFormatBackColor))
                .WithMessage(_ => localiser[CaseWorkflowXPathResources.ColorInvalid])
                .WithErrorCode("ConditionalFormatBackColorInvalid");
        }
    }
}
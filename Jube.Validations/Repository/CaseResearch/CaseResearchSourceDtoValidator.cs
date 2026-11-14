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
using Jube.Dto.Repository.CaseResearch;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseResearch
{
    public sealed class CaseResearchSourceDtoValidator : AbstractValidator<CaseResearchSourceDto>
    {
        public CaseResearchSourceDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.Url)
                .Must(url => CitationAddress.IsWeb(url?.Trim()))
                .WithMessage(_ => string.Format(localiser[CaseResearchResources.UrlInvalid], CitationAddress.MaxLength))
                .WithErrorCode("UrlInvalid");

            RuleFor(p => p.Title)
                .MaximumLength(CitationAddress.MaxTitleLength)
                .WithMessage(_ => string.Format(localiser[CaseResearchResources.TitleMaxLength],
                    CitationAddress.MaxTitleLength))
                .WithErrorCode("TitleMaximumLength");
        }
    }
}

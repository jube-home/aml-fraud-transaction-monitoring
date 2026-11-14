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
    public sealed class CaseResearchDtoValidator : AbstractValidator<CaseResearchDto>
    {
        private const int MaxTopicLength = 500;
        private const int MaxFindingsLength = 100000;

        public CaseResearchDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.Topic)
                .Must(topic => !string.IsNullOrWhiteSpace(topic))
                .WithMessage(_ => localiser[CaseResearchResources.TopicRequired])
                .WithErrorCode("TopicNotEmpty");

            RuleFor(p => p.Topic)
                .MaximumLength(MaxTopicLength)
                .WithMessage(_ => string.Format(localiser[CaseResearchResources.TopicMaxLength], MaxTopicLength))
                .WithErrorCode("TopicMaximumLength");

            RuleFor(p => p.Findings)
                .Must(findings => !string.IsNullOrWhiteSpace(findings))
                .WithMessage(_ => localiser[CaseResearchResources.FindingsRequired])
                .WithErrorCode("FindingsNotEmpty");

            RuleFor(p => p.Findings)
                .MaximumLength(MaxFindingsLength)
                .WithMessage(_ => string.Format(localiser[CaseResearchResources.FindingsMaxLength], MaxFindingsLength))
                .WithErrorCode("FindingsMaximumLength");

            RuleFor(p => p.CaseId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseResearchResources.CaseIdRequired])
                .WithErrorCode("CaseIdGreaterThan");

            RuleFor(p => p.Sources)
                .Must(sources => sources is null || sources.Count <= CitationAddress.MaxPerResearch)
                .WithMessage(_ => string.Format(localiser[CaseResearchResources.TooMany], CitationAddress.MaxPerResearch))
                .WithErrorCode("SourcesMaximumCount");

            RuleForEach(p => p.Sources)
                .Must(source => source is not null)
                .WithErrorCode("SourceNotNull")
                .SetValidator(new CaseResearchSourceDtoValidator(localiser));
        }
    }
}

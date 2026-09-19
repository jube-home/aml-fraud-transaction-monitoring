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
using Jube.Dto.Repository.Archive;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.Archive
{
    public sealed class ArchiveTagDtoValidator : AbstractValidator<ArchiveTagDto>
    {
        private const int MaxTagLength = 256;
        private const int MaxTagCount = 256;

        public ArchiveTagDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelInstanceEntryGuid)
                .NotEmpty()
                .WithMessage(_ => localiser[ArchiveResources.EntityAnalysisModelInstanceEntryGuidRequired])
                .WithErrorCode("EntityAnalysisModelInstanceEntryGuidNotEmpty");

            RuleFor(p => p.Tag)
                .NotNull()
                .WithMessage(_ => localiser[ArchiveResources.TagRequired])
                .WithErrorCode("TagNotNull");

            RuleFor(p => p.Tag)
                .Must(t => t == null || t.Length <= MaxTagCount)
                .WithMessage(_ => string.Format(localiser[ArchiveResources.TagCountMax], MaxTagCount))
                .WithErrorCode("TagCountMaximum");

            RuleForEach(p => p.Tag)
                .NotEmpty()
                .WithMessage(_ => localiser[ArchiveResources.TagNameRequired])
                .WithErrorCode("TagNameNotEmpty")
                .MaximumLength(MaxTagLength)
                .WithMessage(_ => string.Format(localiser[ArchiveResources.TagNameMaxLength], MaxTagLength))
                .WithErrorCode("TagNameMaximumLength")
                .When(p => p.Tag != null);
        }
    }
}
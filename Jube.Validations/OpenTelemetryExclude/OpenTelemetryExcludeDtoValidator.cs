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
using Jube.Dto.OpenTelemetryExclude;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.OpenTelemetryExclude
{
    public sealed class OpenTelemetryExcludeDtoValidator : AbstractValidator<OpenTelemetryExcludeDto>
    {
        private const int MaxNameLength = 255;

        public OpenTelemetryExcludeDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[OpenTelemetryExcludeResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[OpenTelemetryExcludeResources.NameMaxLength],
                    MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .Must(name => name == null || !name.Any(char.IsControl))
                .WithMessage(_ => localiser[OpenTelemetryExcludeResources.NameInvalidCharacters])
                .WithErrorCode("NameInvalidCharacters");
        }
    }
}
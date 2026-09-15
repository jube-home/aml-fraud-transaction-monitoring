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

using System.Text.RegularExpressions;
using FluentValidation;
using Jube.Dto.OpenTelemetryLogCounter;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.OpenTelemetryLogCounter
{
    public sealed class OpenTelemetryLogCounterDtoValidator : AbstractValidator<OpenTelemetryLogCounterDto>
    {
        private const int MaxNameLength = 255;
        private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(1);

        public OpenTelemetryLogCounterDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[OpenTelemetryLogCounterResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[OpenTelemetryLogCounterResources.NameMaxLength],
                    MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .Matches("^[A-Za-z0-9._-]+$")
                .WithMessage(_ => localiser[OpenTelemetryLogCounterResources.NameInvalidCharacters])
                .WithErrorCode("NameInvalidCharacters");

            RuleFor(p => p.Regex)
                .NotEmpty()
                .WithMessage(_ => localiser[OpenTelemetryLogCounterResources.RegexRequired])
                .WithErrorCode("RegexNotEmpty")
                .Must(BeAValidRegex)
                .WithMessage(_ => localiser[OpenTelemetryLogCounterResources.RegexInvalid])
                .WithErrorCode("RegexInvalid")
                .When(p => !string.IsNullOrEmpty(p.Regex));
        }

        private static bool BeAValidRegex(string? pattern)
        {
            try
            {
                _ = new Regex(pattern!, RegexOptions.None, RegexMatchTimeout);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
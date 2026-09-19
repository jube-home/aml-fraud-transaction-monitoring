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
        private const int MaxRegexLength = 2000;
        private static readonly TimeSpan regexMatchTimeout = TimeSpan.FromSeconds(1);

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
                .Matches(@"\A[A-Za-z0-9._-]+\z")
                .WithMessage(_ => localiser[OpenTelemetryLogCounterResources.NameInvalidCharacters])
                .WithErrorCode("NameInvalidCharacters");

            RuleFor(p => p.Regex)
                .NotEmpty()
                .WithMessage(_ => localiser[OpenTelemetryLogCounterResources.RegexRequired])
                .WithErrorCode("RegexNotEmpty")
                .MaximumLength(MaxRegexLength)
                .WithMessage(_ => string.Format(localiser[OpenTelemetryLogCounterResources.RegexTooLong],
                    MaxRegexLength))
                .WithErrorCode("RegexMaximumLength")
                .Must(BeAValidRegex)
                .When(p => !string.IsNullOrEmpty(p.Regex), ApplyConditionTo.CurrentValidator)
                .WithMessage(_ => localiser[OpenTelemetryLogCounterResources.RegexInvalid])
                .WithErrorCode("RegexInvalid");
        }

        private static bool BeAValidRegex(string? pattern)
        {
            if (pattern is null)
            {
                return false;
            }

            try
            {
                _ = new Regex(pattern, RegexOptions.None, regexMatchTimeout);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
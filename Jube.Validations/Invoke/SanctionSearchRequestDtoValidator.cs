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

using System.Globalization;
using FluentValidation;
using Jube.Dto.Invoke;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Invoke
{
    public class SanctionSearchRequestDtoValidator : AbstractValidator<SanctionSearchRequestDto>
    {
        private const int MultiPartStringMaxLength = 500;
        private const int DistanceMaximum = 50;
        private const double MaxDistanceRatioMaximum = 1;
        private const double MaxCoverageRatioMaximum = 100;

        public SanctionSearchRequestDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.MultiPartString)
                .Must(v => !string.IsNullOrWhiteSpace(v))
                .WithMessage(_ => localiser[InvokeResources.MultiPartStringRequired])
                .DependentRules(() =>
                {
                    RuleFor(p => p.MultiPartString)
                        .Must(v => v is not null && v.Trim().Length <= MultiPartStringMaxLength)
                        .WithMessage(_ => localiser[InvokeResources.MultiPartStringTooLong])
                        .Must(v => v is not null && !v.Contains('\0'))
                        .WithMessage(_ => localiser[InvokeResources.MultiPartStringInvalidCharacters]);
                });

            RuleFor(p => p.Distance)
                .Must(v => string.IsNullOrWhiteSpace(v) || IsWholeNumberWithin(v, 0, DistanceMaximum))
                .WithMessage(_ => localiser[InvokeResources.DistanceInvalid]);

            RuleFor(p => p.MaxDistanceRatio)
                .Must(v => string.IsNullOrWhiteSpace(v) || IsFiniteWithin(v, MaxDistanceRatioMaximum))
                .WithMessage(_ => localiser[InvokeResources.MaxDistanceRatioInvalid]);

            RuleFor(p => p.MaxCoverageRatio)
                .Must(v => string.IsNullOrWhiteSpace(v) || IsFiniteWithin(v, MaxCoverageRatioMaximum))
                .WithMessage(_ => localiser[InvokeResources.MaxCoverageRatioInvalid]);
        }

        private static bool IsWholeNumberWithin(string value, int minimum, int maximum)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                   && parsed >= minimum && parsed <= maximum;
        }

        private static bool IsFiniteWithin(string value, double maximum)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                   && double.IsFinite(parsed) && parsed >= 0 && parsed <= maximum;
        }
    }
}
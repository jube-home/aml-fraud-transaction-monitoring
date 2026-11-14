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

using System.Linq;
using FluentValidation;
using Jube.Case.Outbound;
using Jube.Data.Repository;
using Jube.Dto.Repository.TenantRegistry;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.TenantRegistry
{
    public sealed class TenantRegistryDtoValidator : AbstractValidator<TenantRegistryDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxHttpAdaptationUrlLength = 2048;
        private const int MinHttpAdaptationTimeout = 100;
        private const int MaxHttpAdaptationTimeout = 30000;

        public TenantRegistryDtoValidator(TenantRegistryRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[TenantRegistryResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[TenantRegistryResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .Must(HasNoControlOrBidirectionalCharactersAndIsTrimmed)
                .WithMessage(_ => localiser[TenantRegistryResources.NameInvalidCharacters])
                .WithErrorCode("NameInvalidCharacters")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameAsync(name, cancellation);

                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[TenantRegistryResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Active)
                .MustAsync(async (dto, active, cancellation) =>
                {
                    if (active || dto.Id == 0)
                    {
                        return true;
                    }

                    var existing = await repository.GetByIdAsync(dto.Id, cancellation);

                    return existing?.Landlord != 1;
                })
                .WithMessage(_ => localiser[TenantRegistryResources.LandlordMustStayActive])
                .WithErrorCode("LandlordMustStayActive");

            RuleFor(p => p.HttpAdaptationUrl)
                .Must(m => TenantBaseUrlPolicy.Check(m, false) == null)
                .WithMessage(_ => localiser[TenantRegistryResources.HttpAdaptationUrlInvalid])
                .WithErrorCode("HttpAdaptationUrlInvalid")
                .When(w => !string.IsNullOrWhiteSpace(w.HttpAdaptationUrl));

            RuleFor(p => p.HttpAdaptationUrl)
                .MaximumLength(MaxHttpAdaptationUrlLength)
                .WithMessage(_ => localiser[TenantRegistryResources.HttpAdaptationUrlInvalid])
                .WithErrorCode("HttpAdaptationUrlMaximumLength");

            RuleFor(p => p.HttpAdaptationTimeoutMilliseconds)
                .Must(m => m == 0 || m is >= MinHttpAdaptationTimeout and <= MaxHttpAdaptationTimeout)
                .WithMessage(_ => localiser[TenantRegistryResources.HttpAdaptationTimeoutRange])
                .WithErrorCode("HttpAdaptationTimeoutRange");
        }

        private static bool HasNoControlOrBidirectionalCharactersAndIsTrimmed(string? name)
        {
            if (name == null)
            {
                return true;
            }

            return name == name.Trim() && !name.Any(c => char.IsControl(c) || IsBidirectionalControl(c));
        }

        private static bool IsBidirectionalControl(char c)
        {
            return c is '\u061C' or '\u200E' or '\u200F' or (>= '\u202A' and <= '\u202E') or
                (>= '\u2066' and <= '\u2069');
        }
    }
}
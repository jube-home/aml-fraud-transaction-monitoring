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
using Jube.Dto.Repository.UserRegistry;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.UserRegistry
{
    public sealed class UserRegistryDtoValidator : AbstractValidator<UserRegistryDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxEmailLength = 256;

        public UserRegistryDtoValidator(UserRegistryRepository repository,
            RoleRegistryRepository roleRegistryRepository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[UserRegistryResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[UserRegistryResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameAsync(name, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[UserRegistryResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    if (dto.Id > 0)
                    {
                        var current = await repository.GetByIdAsync(dto.Id, cancellation);
                        if (current != null && string.Equals(current.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    return !await repository.NameTakenOutsideTenantAsync(name, cancellation);
                })
                .WithMessage(_ => localiser[UserRegistryResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[UserRegistryResources.EmailRequired])
                .WithErrorCode("EmailNotEmpty")
                .EmailAddress()
                .WithMessage(_ => localiser[UserRegistryResources.EmailInvalid])
                .WithErrorCode("EmailInvalid")
                .MaximumLength(MaxEmailLength)
                .WithMessage(_ => string.Format(localiser[UserRegistryResources.EmailMaxLength], MaxEmailLength))
                .WithErrorCode("EmailMaximumLength");

            RuleFor(p => p.RoleRegistryGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[UserRegistryResources.RoleRegistryGuidRequired])
                .WithErrorCode("RoleRegistryGuidNotEmpty")
                .MustAsync(async (roleRegistryGuid, cancellation) =>
                {
                    var roles = await roleRegistryRepository.GetAsync(cancellation);
                    return roles.Any(r => r.Guid == roleRegistryGuid);
                })
                .WithMessage(_ => localiser[UserRegistryResources.RoleRegistryGuidNotFound])
                .WithErrorCode("RoleRegistryGuidNotFound");
        }
    }
}
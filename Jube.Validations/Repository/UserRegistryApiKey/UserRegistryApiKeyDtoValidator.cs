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
using Jube.Dto.Repository.UserRegistryApiKey;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.UserRegistryApiKey
{
    public sealed class UserRegistryApiKeyDtoValidator : AbstractValidator<UserRegistryApiKeyDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxDescriptionLength = 1024;

        public UserRegistryApiKeyDtoValidator(UserRegistryRepository userRegistryRepository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.UserRegistryId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[UserRegistryApiKeyResources.UserRegistryIdInvalid])
                .WithErrorCode("UserRegistryIdInvalid")
                .MustAsync(async (userRegistryId, cancellation) =>
                    await userRegistryRepository.GetByIdAsync(userRegistryId, cancellation) is not null)
                .WithMessage(_ => localiser[UserRegistryApiKeyResources.UserRegistryNotFound])
                .WithErrorCode("UserRegistryIdNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[UserRegistryApiKeyResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[UserRegistryApiKeyResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength");

            RuleFor(p => p.Description)
                .MaximumLength(MaxDescriptionLength)
                .WithMessage(_ =>
                    string.Format(localiser[UserRegistryApiKeyResources.DescriptionMaxLength], MaxDescriptionLength))
                .WithErrorCode("DescriptionMaximumLength");
        }
    }
}
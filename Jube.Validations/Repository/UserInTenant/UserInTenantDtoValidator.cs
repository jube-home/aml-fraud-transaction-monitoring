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
using Jube.Dto.Repository.UserInTenant;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.UserInTenant
{
    public sealed class UserInTenantDtoValidator : AbstractValidator<UserInTenantDto>
    {
        public UserInTenantDtoValidator(UserInTenantRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.TenantRegistryId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[UserInTenantResources.TenantRegistryIdInvalid])
                .WithErrorCode("TenantRegistryIdInvalid")
                .MustAsync(async (tenantRegistryId, cancellation) =>
                    await repository.ExistsTenantRegistryAsync(tenantRegistryId, cancellation))
                .WithMessage(_ => localiser[UserInTenantResources.TenantRegistryIdNotFound])
                .WithErrorCode("TenantRegistryIdNotFound");
        }
    }
}
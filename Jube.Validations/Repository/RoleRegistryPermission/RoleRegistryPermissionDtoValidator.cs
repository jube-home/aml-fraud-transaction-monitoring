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
using Jube.Dto.Repository.RoleRegistryPermission;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.RoleRegistryPermission
{
    public sealed class RoleRegistryPermissionDtoValidator : AbstractValidator<RoleRegistryPermissionDto>
    {
        public RoleRegistryPermissionDtoValidator(RoleRegistryPermissionRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.RoleRegistryId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[RoleRegistryPermissionResources.RoleRegistryIdInvalid])
                .WithErrorCode("RoleRegistryIdInvalid")
                .MustAsync(async (roleRegistryId, cancellation) =>
                    await repository.ExistsRoleRegistryAsync(roleRegistryId, cancellation))
                .WithMessage(_ => localiser[RoleRegistryPermissionResources.RoleRegistryNotFound])
                .WithErrorCode("RoleRegistryIdNotFound");

            RuleFor(p => p.PermissionSpecificationId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[RoleRegistryPermissionResources.PermissionSpecificationIdInvalid])
                .WithErrorCode("PermissionSpecificationIdInvalid")
                .MustAsync(async (permissionSpecificationId, cancellation) =>
                    await repository.ExistsPermissionSpecificationAsync(permissionSpecificationId, cancellation))
                .WithMessage(_ => localiser[RoleRegistryPermissionResources.PermissionSpecificationNotFound])
                .WithErrorCode("PermissionSpecificationIdNotFound")
                .MustAsync(async (dto, permissionSpecificationId, cancellation) =>
                {
                    var existing = await repository
                        .GetByRoleRegistryIdOrderByIdAsync(dto.RoleRegistryId, cancellation)
                        .ConfigureAwait(false);
                    return existing.All(e =>
                        e.Id == dto.Id || e.PermissionSpecificationId != permissionSpecificationId);
                })
                .WithMessage(_ => localiser[RoleRegistryPermissionResources.PermissionSpecificationAlreadyGranted])
                .WithErrorCode("PermissionSpecificationIdDuplicate");
        }
    }
}
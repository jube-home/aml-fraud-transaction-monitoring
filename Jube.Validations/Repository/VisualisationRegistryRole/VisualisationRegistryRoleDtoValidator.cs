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
using Jube.Dto.Repository.VisualisationRegistryRole;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.VisualisationRegistryRole
{
    public sealed class VisualisationRegistryRoleDtoValidator : AbstractValidator<VisualisationRegistryRoleDto>
    {
        public VisualisationRegistryRoleDtoValidator(VisualisationRegistryRoleRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.VisualisationRegistryGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[VisualisationRegistryRoleResources.VisualisationRegistryGuidRequired])
                .WithErrorCode("VisualisationRegistryGuidNotEmpty")
                .MustAsync(async (visualisationRegistryGuid, cancellation) =>
                    await repository.ExistsVisualisationRegistryAsync(visualisationRegistryGuid, cancellation))
                .WithMessage(_ => localiser[VisualisationRegistryRoleResources.VisualisationRegistryNotFound])
                .WithErrorCode("VisualisationRegistryGuidNotFound");

            RuleFor(p => p.RoleRegistryGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[VisualisationRegistryRoleResources.RoleRegistryGuidRequired])
                .WithErrorCode("RoleRegistryGuidNotEmpty")
                .MustAsync(async (roleRegistryGuid, cancellation) =>
                    await repository.ExistsRoleRegistryAsync(roleRegistryGuid, cancellation))
                .WithMessage(_ => localiser[VisualisationRegistryRoleResources.RoleRegistryNotFound])
                .WithErrorCode("RoleRegistryGuidNotFound");
        }
    }
}
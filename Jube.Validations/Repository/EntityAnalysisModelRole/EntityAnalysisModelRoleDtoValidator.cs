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
using Jube.Dto.Repository.EntityAnalysisModelRole;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.EntityAnalysisModelRole
{
    public sealed class EntityAnalysisModelRoleDtoValidator : AbstractValidator<EntityAnalysisModelRoleDto>
    {
        public EntityAnalysisModelRoleDtoValidator(EntityAnalysisModelRoleRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelRoleResources.EntityAnalysisModelGuidRequired])
                .WithErrorCode("EntityAnalysisModelGuidNotEmpty")
                .MustAsync(async (entityAnalysisModelGuid, cancellation) =>
                    await repository.ExistsEntityAnalysisModelAsync(entityAnalysisModelGuid, cancellation))
                .WithMessage(_ => localiser[EntityAnalysisModelRoleResources.EntityAnalysisModelNotFound])
                .WithErrorCode("EntityAnalysisModelGuidNotFound");

            RuleFor(p => p.RoleRegistryGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelRoleResources.RoleRegistryGuidRequired])
                .WithErrorCode("RoleRegistryGuidNotEmpty")
                .MustAsync(async (roleRegistryGuid, cancellation) =>
                    await repository.ExistsRoleRegistryAsync(roleRegistryGuid, cancellation))
                .WithMessage(_ => localiser[EntityAnalysisModelRoleResources.RoleRegistryNotFound])
                .WithErrorCode("RoleRegistryGuidNotFound");
        }
    }
}
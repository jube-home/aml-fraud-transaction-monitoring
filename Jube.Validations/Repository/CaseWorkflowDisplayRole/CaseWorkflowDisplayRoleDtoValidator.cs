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
using Jube.Dto.Repository.CaseWorkflowDisplayRole;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowDisplayRole
{
    public sealed class CaseWorkflowDisplayRoleDtoValidator : AbstractValidator<CaseWorkflowDisplayRoleDto>
    {
        public CaseWorkflowDisplayRoleDtoValidator(CaseWorkflowDisplayRoleRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowDisplayGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowDisplayRoleResources.CaseWorkflowDisplayGuidRequired])
                .WithErrorCode("CaseWorkflowDisplayGuidNotEmpty")
                .MustAsync(async (caseWorkflowDisplayGuid, cancellation) =>
                    await repository.ExistsCaseWorkflowDisplayAsync(caseWorkflowDisplayGuid, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowDisplayRoleResources.CaseWorkflowDisplayNotFound])
                .WithErrorCode("CaseWorkflowDisplayGuidNotFound");

            RuleFor(p => p.RoleRegistryGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowDisplayRoleResources.RoleRegistryGuidRequired])
                .WithErrorCode("RoleRegistryGuidNotEmpty")
                .MustAsync(async (roleRegistryGuid, cancellation) =>
                    await repository.ExistsRoleRegistryAsync(roleRegistryGuid, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowDisplayRoleResources.RoleRegistryNotFound])
                .WithErrorCode("RoleRegistryGuidNotFound");
        }
    }
}
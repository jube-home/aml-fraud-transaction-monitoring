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
using Jube.Dto.Repository.CaseWorkflowStatusRole;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowStatusRole
{
    public sealed class CaseWorkflowStatusRoleDtoValidator : AbstractValidator<CaseWorkflowStatusRoleDto>
    {
        public CaseWorkflowStatusRoleDtoValidator(CaseWorkflowStatusRoleRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.CaseWorkflowStatusGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowStatusRoleResources.CaseWorkflowStatusGuidRequired])
                .WithErrorCode("CaseWorkflowStatusGuidNotEmpty")
                .MustAsync(async (caseWorkflowStatusGuid, cancellation) =>
                    await repository.ExistsCaseWorkflowStatusAsync(caseWorkflowStatusGuid, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowStatusRoleResources.CaseWorkflowStatusNotFound])
                .WithErrorCode("CaseWorkflowStatusGuidNotFound");

            RuleFor(p => p.RoleRegistryGuid)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowStatusRoleResources.RoleRegistryGuidRequired])
                .WithErrorCode("RoleRegistryGuidNotEmpty")
                .MustAsync(async (roleRegistryGuid, cancellation) =>
                    await repository.ExistsRoleRegistryAsync(roleRegistryGuid, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowStatusRoleResources.RoleRegistryNotFound])
                .WithErrorCode("RoleRegistryGuidNotFound");
        }
    }
}
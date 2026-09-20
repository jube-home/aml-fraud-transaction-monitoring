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
using Jube.Dto.Repository.SessionCaseSearchCompiledSql;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.SessionCaseSearchCompiledSql
{
    public sealed class SessionCaseSearchCompiledSqlDtoValidator : AbstractValidator<SessionCaseSearchCompiledSqlDto>
    {
        public SessionCaseSearchCompiledSqlDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.SelectJson).NotEmpty()
                .WithMessage(_ => localiser[SessionCaseSearchCompiledSqlResources.SelectJsonRequired])
                .WithErrorCode("SelectJsonNotEmpty");
            RuleFor(p => p.FilterJson).NotEmpty()
                .WithMessage(_ => localiser[SessionCaseSearchCompiledSqlResources.FilterJsonRequired])
                .WithErrorCode("FilterJsonNotEmpty");
            RuleFor(p => p.CaseWorkflowGuid).NotEmpty()
                .WithMessage(_ => localiser[SessionCaseSearchCompiledSqlResources.CaseWorkflowGuidRequired])
                .WithErrorCode("CaseWorkflowGuidNotEmpty");
        }
    }
}
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
using Jube.App.Dto;

namespace Jube.App.Validators
{
    public class CaseDtoValidator : AbstractValidator<CaseDto>
    {
        public CaseDtoValidator()
        {
            RuleFor(p => p.Id).GreaterThan(0);
            RuleFor(p => p.ClosedStatusId).GreaterThanOrEqualTo((byte)0);
            RuleFor(p => p.Locked).NotNull();
            RuleFor(p => p.LockedUser).NotEmpty().When(w => w.Locked);
            RuleFor(p => p.CaseWorkflowStatusGuid).NotEmpty();
            RuleFor(p => p.Diary).NotNull();
            RuleFor(p => p.DiaryDate).NotEmpty().When(w => w.Diary);
            RuleFor(p => p.Rating).GreaterThan((byte)0);
            RuleFor(p => p.Payload).NotEmpty();
        }
    }
}
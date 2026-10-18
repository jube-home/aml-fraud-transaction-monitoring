/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using FluentValidation;
using Jube.Dto.AskJoobyMemory;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.AskJoobyMemory
{
    public sealed class AskJoobyMemoryDtoValidator : AbstractValidator<AskJoobyMemoryDto>
    {
        private const int MaxContentLength = 4000;
        private static readonly string[] validScopes = ["User", "System"];

        public AskJoobyMemoryDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.Content)
                .NotEmpty()
                .WithMessage(_ => localiser[AskJoobyMemoryResources.ContentRequired])
                .WithErrorCode("ContentRequired")
                .MaximumLength(MaxContentLength)
                .WithMessage(_ => string.Format(localiser[AskJoobyMemoryResources.ContentTooLong], MaxContentLength))
                .WithErrorCode("ContentTooLong");

            RuleFor(p => p.Scope)
                .Must(scope => scope != null && validScopes.Contains(scope))
                .WithMessage(_ => localiser[AskJoobyMemoryResources.InvalidScope])
                .WithErrorCode("InvalidScope");
        }
    }
}
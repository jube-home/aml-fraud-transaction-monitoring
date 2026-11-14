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
using Jube.Dto.Repository.GraphRegistry;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.GraphRegistry
{
    public sealed class GraphRegistryDtoValidator : AbstractValidator<GraphRegistryDto>
    {
        private const int MaxNameLength = 256;

        public GraphRegistryDtoValidator(GraphRegistryRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[GraphRegistryResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[GraphRegistryResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameAsync(name, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[GraphRegistryResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.ContextKindId)
                .InclusiveBetween(1, 2)
                .WithMessage(_ => localiser[GraphRegistryResources.ContextKindInvalid])
                .WithErrorCode("ContextKindInvalid");

            RuleFor(p => p.DefaultLayoutId)
                .InclusiveBetween(1, 4)
                .WithMessage(_ => localiser[GraphRegistryResources.LayoutInvalid])
                .WithErrorCode("LayoutInvalid");

            RuleFor(p => p.DefaultHops)
                .InclusiveBetween(1, 3)
                .WithMessage(_ => localiser[GraphRegistryResources.DefaultHopsOutOfRange])
                .WithErrorCode("DefaultHopsOutOfRange");

            RuleFor(p => p.DefaultWindowDays)
                .InclusiveBetween(1, 3650)
                .WithMessage(_ => localiser[GraphRegistryResources.DefaultWindowDaysOutOfRange])
                .WithErrorCode("DefaultWindowDaysOutOfRange");

            RuleFor(p => p.RenderCap)
                .InclusiveBetween(10, 150)
                .WithMessage(_ => localiser[GraphRegistryResources.RenderCapOutOfRange])
                .WithErrorCode("RenderCapOutOfRange");

            RuleFor(p => p.HubPolicyId)
                .InclusiveBetween(1, 2)
                .WithMessage(_ => localiser[GraphRegistryResources.HubPolicyInvalid])
                .WithErrorCode("HubPolicyInvalid");
        }
    }
}

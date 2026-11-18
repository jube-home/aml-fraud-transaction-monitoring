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
using Jube.Charting.Diagrams.Icons;
using Jube.Data.Repository;
using Jube.Dto.Repository.GraphRegistryEdge;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.GraphRegistryEdge
{
    public sealed class GraphRegistryEdgeDtoValidator : AbstractValidator<GraphRegistryEdgeDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxKeyLength = 128;

        private static bool IsKnownIcon(string? icon) => string.IsNullOrWhiteSpace(icon) || GraphIcons.Exists(icon.Trim());

        public GraphRegistryEdgeDtoValidator(GraphRegistryEdgeRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.GraphRegistryId)
                .MustAsync(async (graphRegistryId, cancellation) =>
                    await repository.ExistsGraphRegistryAsync(graphRegistryId, cancellation))
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.GraphRegistryNotFound])
                .WithErrorCode("GraphRegistryNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[GraphRegistryEdgeResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameGraphRegistryIdAsync(name, dto.GraphRegistryId,
                        cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.KindId)
                .InclusiveBetween(1, 3)
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.KindInvalid])
                .WithErrorCode("KindInvalid");

            RuleFor(p => p.FromKey)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.FromKeyRequired])
                .WithErrorCode("FromKeyNotEmpty")
                .MaximumLength(MaxKeyLength)
                .WithMessage(_ => string.Format(localiser[GraphRegistryEdgeResources.KeyMaxLength], MaxKeyLength))
                .WithErrorCode("FromKeyMaximumLength");

            RuleFor(p => p.ToKey)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.ToKeyRequired])
                .WithErrorCode("ToKeyNotEmpty")
                .MaximumLength(MaxKeyLength)
                .WithMessage(_ => string.Format(localiser[GraphRegistryEdgeResources.KeyMaxLength], MaxKeyLength))
                .WithErrorCode("ToKeyMaximumLength")
                .Must((dto, toKey) => !string.Equals(dto.FromKey, toKey, StringComparison.OrdinalIgnoreCase))
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.KeysMustDiffer])
                .WithErrorCode("KeysMustDiffer");

            RuleFor(p => p.FromNodeKey)
                .MaximumLength(MaxKeyLength)
                .WithMessage(_ => string.Format(localiser[GraphRegistryEdgeResources.KeyMaxLength], MaxKeyLength))
                .WithErrorCode("FromNodeKeyMaximumLength");

            RuleFor(p => p.ToNodeKey)
                .MaximumLength(MaxKeyLength)
                .WithMessage(_ => string.Format(localiser[GraphRegistryEdgeResources.KeyMaxLength], MaxKeyLength))
                .WithErrorCode("ToNodeKeyMaximumLength");

            RuleFor(p => p.IconName)
                .Must(IsKnownIcon)
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.IconInvalid])
                .WithErrorCode("IconInvalid");

            RuleFor(p => p.FromNodeIcon)
                .Must(IsKnownIcon)
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.IconInvalid])
                .WithErrorCode("FromNodeIconInvalid");

            RuleFor(p => p.ToNodeIcon)
                .Must(IsKnownIcon)
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.IconInvalid])
                .WithErrorCode("ToNodeIconInvalid");

            RuleFor(p => p.AmountKey)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .When(p => p.KindId == 2)
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.AmountKeyRequired])
                .WithErrorCode("AmountKeyNotEmpty")
                .MaximumLength(MaxKeyLength)
                .WithMessage(_ => string.Format(localiser[GraphRegistryEdgeResources.KeyMaxLength], MaxKeyLength))
                .WithErrorCode("AmountKeyMaximumLength");

            RuleFor(p => p.HalfLifeDays)
                .InclusiveBetween(1, 3650)
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.HalfLifeOutOfRange])
                .WithErrorCode("HalfLifeOutOfRange");

            RuleFor(p => p.KindPrior)
                .InclusiveBetween(0, 1)
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.KindPriorOutOfRange])
                .WithErrorCode("KindPriorOutOfRange");

            RuleFor(p => p.HubThreshold)
                .GreaterThanOrEqualTo(2)
                .When(p => p.HubThreshold.HasValue)
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.HubThresholdOutOfRange])
                .WithErrorCode("HubThresholdOutOfRange");

            RuleFor(p => p.MinimumAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[GraphRegistryEdgeResources.MinimumAmountNegative])
                .WithErrorCode("MinimumAmountNegative");
        }
    }
}

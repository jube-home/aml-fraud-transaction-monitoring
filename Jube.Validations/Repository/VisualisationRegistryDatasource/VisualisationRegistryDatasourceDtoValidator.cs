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
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.VisualisationRegistryDatasource
{
    public sealed class
        VisualisationRegistryDatasourceDtoValidator : AbstractValidator<VisualisationRegistryDatasourceDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxCommandLength = 65536;
        private const int MaxVisualisationTextLength = 65536;

        private static readonly int[] allowedVisualisationTypeIds = [1, 2, 3];

        public VisualisationRegistryDatasourceDtoValidator(
            VisualisationRegistryDatasourceRepository repository,
            VisualisationRegistryRepository visualisationRegistryRepository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.VisualisationRegistryId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.VisualisationRegistryIdInvalid])
                .WithErrorCode("VisualisationRegistryIdInvalid")
                .MustAsync(async (visualisationRegistryId, cancellation) =>
                {
                    var parent = await visualisationRegistryRepository
                        .GetByIdAsync(visualisationRegistryId, cancellation);
                    return parent != null;
                })
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.VisualisationRegistryIdNotFound])
                .WithErrorCode("VisualisationRegistryIdNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[VisualisationRegistryDatasourceResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameVisualisationRegistryIdAsync(name, dto.VisualisationRegistryId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Command)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.CommandRequired])
                .WithErrorCode("CommandNotEmpty")
                .MaximumLength(MaxCommandLength)
                .WithMessage(_ =>
                    string.Format(localiser[VisualisationRegistryDatasourceResources.CommandMaxLength],
                        MaxCommandLength))
                .WithErrorCode("CommandMaximumLength");

            RuleFor(p => p.Priority)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.PriorityRange])
                .WithErrorCode("PriorityRange");

            RuleFor(p => p.ColumnSpan)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.ColumnSpanRange])
                .WithErrorCode("ColumnSpanRange");

            RuleFor(p => p.RowSpan)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.RowSpanRange])
                .WithErrorCode("RowSpanRange");

            RuleFor(p => p.VisualisationTypeId)
                .Must(m => allowedVisualisationTypeIds.Contains(m))
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.VisualisationTypeIdInvalid])
                .WithErrorCode("VisualisationTypeIdInvalid")
                .When(w => w.IncludeDisplay);

            RuleFor(p => p.VisualisationText)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.VisualisationTextRequired])
                .WithErrorCode("VisualisationTextNotEmpty")
                .MaximumLength(MaxVisualisationTextLength)
                .WithMessage(_ =>
                    string.Format(localiser[VisualisationRegistryDatasourceResources.VisualisationTextMaxLength],
                        MaxVisualisationTextLength))
                .WithErrorCode("VisualisationTextMaximumLength")
                .When(w => w.IncludeDisplay);
        }
    }
}
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
using Jube.Dto.Repository.VisualisationRegistry;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.VisualisationRegistry
{
    public sealed class VisualisationRegistryDtoValidator : AbstractValidator<VisualisationRegistryDto>
    {
        private const int MaxNameLength = 256;

        public VisualisationRegistryDtoValidator(VisualisationRegistryRepository repository,
            IStringLocalizer localiser)
        {
            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[VisualisationRegistryResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[VisualisationRegistryResources.NameMaxLength],
                    MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameAsync(name, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[VisualisationRegistryResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Columns)
                .GreaterThan(0)
                .WithMessage(_ => localiser[VisualisationRegistryResources.ColumnsInvalid])
                .WithErrorCode("ColumnsInvalid");

            RuleFor(p => p.ColumnWidth)
                .GreaterThan(0)
                .WithMessage(_ => localiser[VisualisationRegistryResources.ColumnWidthInvalid])
                .WithErrorCode("ColumnWidthInvalid");

            RuleFor(p => p.RowHeight)
                .GreaterThan(0)
                .WithMessage(_ => localiser[VisualisationRegistryResources.RowHeightInvalid])
                .WithErrorCode("RowHeightInvalid");
        }
    }
}
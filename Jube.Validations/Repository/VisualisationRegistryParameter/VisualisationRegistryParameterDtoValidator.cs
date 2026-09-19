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

using System.Globalization;
using FluentValidation;
using Jube.Data.Repository;
using Jube.Dto.Repository.VisualisationRegistryParameter;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.VisualisationRegistryParameter
{
    public sealed class VisualisationRegistryParameterDtoValidator
        : AbstractValidator<VisualisationRegistryParameterDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxDefaultValueLength = 256;
        private static readonly int[] dataTypeIds = [1, 2, 3, 4, 5];

        public VisualisationRegistryParameterDtoValidator(
            VisualisationRegistryRepository visualisationRegistryRepository,
            VisualisationRegistryParameterRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.VisualisationRegistryId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[VisualisationRegistryParameterResources.VisualisationRegistryIdInvalid])
                .WithErrorCode("VisualisationRegistryIdInvalid")
                .MustAsync(async (visualisationRegistryId, cancellation) =>
                    await visualisationRegistryRepository.GetByIdAsync(visualisationRegistryId, cancellation) != null)
                .WithMessage(_ => localiser[VisualisationRegistryParameterResources.VisualisationRegistryIdInvalid])
                .WithErrorCode("VisualisationRegistryIdNotFound");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[VisualisationRegistryParameterResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[VisualisationRegistryParameterResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameVisualisationRegistryIdAsync(name, dto.VisualisationRegistryId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[VisualisationRegistryParameterResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.DataTypeId)
                .Cascade(CascadeMode.Stop)
                .Must(m => dataTypeIds.Contains(m))
                .WithMessage(_ => localiser[VisualisationRegistryParameterResources.DataTypeIdInvalid])
                .WithErrorCode("DataTypeIdInvalid");

            RuleFor(p => p.DefaultValue)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[VisualisationRegistryParameterResources.DefaultValueRequired])
                .WithErrorCode("DefaultValueNotEmpty")
                .MaximumLength(MaxDefaultValueLength)
                .WithMessage(_ =>
                    string.Format(localiser[VisualisationRegistryParameterResources.DefaultValueMaxLength],
                        MaxDefaultValueLength))
                .WithErrorCode("DefaultValueMaximumLength")
                .Must((dto, defaultValue) => IsDefaultValueValidForDataType(dto.DataTypeId, defaultValue))
                .WithMessage(_ => localiser[VisualisationRegistryParameterResources.DefaultValueFormatInvalid])
                .WithErrorCode("DefaultValueFormatInvalid");
        }

        private static bool IsDefaultValueValidForDataType(int dataTypeId, string? defaultValue)
        {
            return dataTypeId switch
            {
                2 => int.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                3 => double.TryParse(defaultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                4 => int.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                5 => byte.TryParse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                _ => true
            };
        }
    }
}
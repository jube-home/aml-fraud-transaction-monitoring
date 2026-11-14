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
using Jube.Dto.Options;
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
        public const int MaxCommandLength = 65536;
        private const int MaxTemplateTextLength = 65536;
        private const int MaxMappingColumnNameLength = 128;

        private static T? Maybe<T>(T? value) where T : class
        {
            return value;
        }

        private static List<DatasourceMappingEntryDto> Entries(List<DatasourceMappingEntryDto>? mapping)
        {
            return (mapping ?? []).Where(e => Maybe(e) is not null).ToList();
        }

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

            RuleFor(p => p.DisplayKindId)
                .Must(m => m == 0 || Enum.IsDefined(typeof(DatasourceDisplayKind), m))
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.DisplayKindIdInvalid])
                .WithErrorCode("DisplayKindIdInvalid");

            RuleFor(p => p.TemplateText)
                .MaximumLength(MaxTemplateTextLength)
                .WithMessage(_ =>
                    string.Format(localiser[VisualisationRegistryDatasourceResources.TemplateTextMaxLength],
                        MaxTemplateTextLength))
                .WithErrorCode("TemplateTextMaximumLength");

            RuleFor(p => p.Mapping)
                .Must(m => Entries(m).Select(e => e.RoleId).All(r => Enum.IsDefined(typeof(DatasourceChartRole), r)))
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.MappingRoleInvalid])
                .WithErrorCode("MappingRoleInvalid")
                .Must(m => Entries(m).Select(e => e.RoleId).Distinct().Count() == Entries(m).Count)
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.MappingRoleDuplicate])
                .WithErrorCode("MappingRoleDuplicate")
                .Must(m => Entries(m).SelectMany(e => Maybe(e.Columns) ?? []).All(c =>
                    !string.IsNullOrWhiteSpace(c) && c.Length <= MaxMappingColumnNameLength))
                .WithMessage(_ =>
                    string.Format(localiser[VisualisationRegistryDatasourceResources.MappingColumnInvalid],
                        MaxMappingColumnNameLength))
                .WithErrorCode("MappingColumnInvalid")
                .Must(m =>
                {
                    var columns = Entries(m).SelectMany(e => Maybe(e.Columns) ?? []).ToList();

                    return columns.Distinct(StringComparer.Ordinal).Count() == columns.Count;
                })
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.MappingColumnDuplicate])
                .WithErrorCode("MappingColumnDuplicate");

            RuleFor(p => p.DisplayKindId)
                .Must(m => m > 0)
                .WithMessage(_ => localiser[VisualisationRegistryDatasourceResources.DisplayRequired])
                .WithErrorCode("DisplayRequired")
                .When(w => !w.IncludeGrid);
        }
    }
}
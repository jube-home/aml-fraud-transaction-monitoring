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
using FluentValidation.Results;
using Jube.Analytics.Datasets;
using Jube.Analytics.Datasets.Models;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.ExhaustiveInstanceDataset;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.ExhaustiveInstanceDataset
{
    public sealed class ExhaustiveInstanceDatasetMappingDtoValidator
        : AbstractValidator<ExhaustiveInstanceDatasetMappingDto>
    {
        public const int MaxNameLength = 256;

        public ExhaustiveInstanceDatasetMappingDtoValidator(IReadOnlyList<DatasetColumnDefinition> profile,
            ExhaustiveInstanceDatasetRepository repository, DatasetKind kind, IStringLocalizer localiser)
        {
            RuleFor(p => p.Name)
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[ExhaustiveInstanceDatasetResources.NameMaxLength],
                    MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .Must(NameText.IsAcceptable)
                .WithMessage(_ => localiser[ExhaustiveInstanceDatasetResources.NameRequired])
                .WithErrorCode("NameInvalid")
                .MustAsync(async (dto, name, cancellation) =>
                    !await repository.NameExistsAsync(name!.Trim(), kind, dto.Id, cancellation))
                .WithMessage(_ => localiser[ExhaustiveInstanceDatasetResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate")
                .When(p => !string.IsNullOrWhiteSpace(p.Name));

            RuleFor(p => p).Custom((dto, context) =>
            {
                var input = new DatasetMappingInput(
                    dto.Columns?.Select(c => new DatasetMappingColumn(c.Index, c.Include, c.Name,
                        c.ProcessingTypeId)).ToList());

                foreach (var issue in DatasetMappingRules.Validate(profile, input))
                {
                    context.AddFailure(new ValidationFailure(
                        issue.ColumnIndex.HasValue ? $"Columns[{issue.ColumnIndex.Value}]" : "Columns",
                        localiser["Mapping" + issue.Code])
                    {
                        ErrorCode = "Mapping" + issue.Code
                    });
                }
            });
        }
    }
}
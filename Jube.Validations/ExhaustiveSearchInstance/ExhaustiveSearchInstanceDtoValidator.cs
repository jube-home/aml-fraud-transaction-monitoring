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
using Jube.Analytics.Anomaly;
using Jube.Analytics.Anomaly.Models;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.ExhaustiveSearchInstance;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.ExhaustiveSearchInstance
{
    public sealed class ExhaustiveSearchInstanceDtoValidator : AbstractValidator<ExhaustiveSearchInstanceDto>
    {
        private const int MaxNameLength = 256;

        public ExhaustiveSearchInstanceDtoValidator(ExhaustiveSearchInstanceRepository repository,
            IStringLocalizer localiser, ExhaustiveInstanceDatasetRepository? datasetRepository = null)
        {
            Include(new FiniteNumberValidator<ExhaustiveSearchInstanceDto>());

            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.EntityAnalysisModelId)
                .MustAsync(repository.ParentModelVisibleAsync)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdNotFound")
                .When(p => p.Id == 0 && p.EntityAnalysisModelId > 0);

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[ExhaustiveSearchInstanceResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);

                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.DataSourceId)
                .Must(v => v is null || (v is >= 0 and <= 255 && Enum.IsDefined((ExhaustiveDataSource)(byte)v.Value)))
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.DataSourceInvalid])
                .WithErrorCode("DataSourceInvalid");

            RuleFor(p => p.CalibrationMethod)
                .Must(v => v is null or "automatic" or "platt" or "isotonic" or "beta")
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.CalibrationMethodInvalid])
                .WithErrorCode("CalibrationMethodInvalid");

            RuleFor(p => p)
                .Must(p => p.ExhaustiveInstanceCsvId.HasValue || p.ExhaustiveInstanceJsonId.HasValue)
                .WithName(nameof(ExhaustiveSearchInstanceDto.ExhaustiveInstanceCsvId))
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.DatasetRequired])
                .WithErrorCode("DatasetRequired")
                .When(p => p.DataSourceId == (int)ExhaustiveDataSource.UploadedDataset);

            RuleFor(p => p)
                .Must(p => !(p.ExhaustiveInstanceCsvId.HasValue && p.ExhaustiveInstanceJsonId.HasValue))
                .WithName(nameof(ExhaustiveSearchInstanceDto.ExhaustiveInstanceCsvId))
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.DatasetAmbiguous])
                .WithErrorCode("DatasetAmbiguous")
                .When(p => p.DataSourceId == (int)ExhaustiveDataSource.UploadedDataset);

            RuleFor(p => p)
                .MustAsync(async (p, cancellation) =>
                {
                    if (datasetRepository == null)
                    {
                        return false;
                    }

                    return p.ExhaustiveInstanceCsvId.HasValue
                        ? await datasetRepository.IsConfirmedAsync(DatasetKind.Csv, p.ExhaustiveInstanceCsvId.Value,
                            cancellation)
                        : await datasetRepository.IsConfirmedAsync(DatasetKind.Json,
                            p.ExhaustiveInstanceJsonId.GetValueOrDefault(), cancellation);
                })
                .WithName(nameof(ExhaustiveSearchInstanceDto.ExhaustiveInstanceCsvId))
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.DatasetNotFound])
                .WithErrorCode("DatasetNotFound")
                .When(p => p.DataSourceId == (int)ExhaustiveDataSource.UploadedDataset
                           && p.ExhaustiveInstanceCsvId.HasValue != p.ExhaustiveInstanceJsonId.HasValue);

            RuleFor(p => p)
                .Must(p => !p.ExhaustiveInstanceCsvId.HasValue && !p.ExhaustiveInstanceJsonId.HasValue)
                .WithName(nameof(ExhaustiveSearchInstanceDto.ExhaustiveInstanceCsvId))
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.DatasetNotPermitted])
                .WithErrorCode("DatasetNotPermitted")
                .When(p => p.DataSourceId != (int)ExhaustiveDataSource.UploadedDataset);

            RuleFor(p => p.AnomalyProbability)
                .InclusiveBetween(0, 1)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.AnomalyProbabilityRange])
                .WithErrorCode("AnomalyProbabilityRange")
                .When(w => w.Anomaly && IsOneClassSvm(w));

            RuleFor(p => p.AnomalyProbability)
                .Must((dto, probability) => ContaminationAcceptable(dto, probability))
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.AnomalyContaminationRange])
                .WithErrorCode("AnomalyContaminationRange")
                .When(w => w.Anomaly && !IsOneClassSvm(w));

            RuleFor(p => p.AnomalyAlgorithmId)
                .Must(id => !id.HasValue || Enum.IsDefined(typeof(AnomalyAlgorithm), id.Value))
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.AnomalyAlgorithmInvalid])
                .WithErrorCode("AnomalyAlgorithmInvalid");

            RuleFor(p => p.AnomalyParametersJson)
                .Must(json => string.IsNullOrWhiteSpace(json) ||
                              AnomalyParameters.TryParse(json, out _, out _))
                .WithMessage((_, json) => string.Format(
                    localiser[ExhaustiveSearchInstanceResources.AnomalyParametersInvalid],
                    ParameterErrors(json)))
                .WithErrorCode("AnomalyParametersInvalid");

            RuleFor(p => p.BaseRate)
                .Must(v => v is null || (v > 0 && v < 1))
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.BaseRateRange])
                .WithErrorCode("BaseRateRange");

            RuleFor(p => p.FilterJson)
                .NotEmpty()
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.FilterJsonRequired])
                .WithErrorCode("FilterJsonNotEmpty")
                .When(w => w.Filter);

            RuleFor(p => p.FilterJson)
                .Must(JsonText.IsAcceptable)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.FilterJsonInvalid])
                .WithErrorCode("FilterJsonInvalid");

            RuleFor(p => p.CategoricalOptionsJson)
                .Must(JsonText.IsAcceptable)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.CategoricalOptionsInvalid])
                .WithErrorCode("CategoricalOptionsInvalid");

            RuleFor(p => p.FilterSql)
                .NotEmpty()
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.FilterSqlRequired])
                .WithErrorCode("FilterSqlNotEmpty")
                .When(w => w.Filter);

            RuleFor(p => p.FilterSql)
                .Must(FilterSqlText.IsAcceptable)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.FilterSqlInvalid])
                .WithErrorCode("FilterSqlInvalid");

            RuleFor(p => p.FilterTokens)
                .NotEmpty()
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.FilterTokensRequired])
                .WithErrorCode("FilterTokensNotEmpty")
                .When(w => w.Filter);

            RuleFor(p => p.FilterTokens)
                .Must(FilterSqlText.IsAcceptableTokens)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceResources.FilterTokensInvalid])
                .WithErrorCode("FilterTokensInvalid");
        }

        private static bool IsOneClassSvm(ExhaustiveSearchInstanceDto dto)
        {
            return !dto.AnomalyAlgorithmId.HasValue || dto.AnomalyAlgorithmId.Value == 0;
        }

        private static bool ContaminationAcceptable(ExhaustiveSearchInstanceDto dto, double probability)
        {
            if (!string.IsNullOrWhiteSpace(dto.AnomalyParametersJson) &&
                AnomalyParameters.TryParse(dto.AnomalyParametersJson, out var parameters, out _) &&
                parameters.Contamination.HasValue)
            {
                return true;
            }

            return probability > 0 && probability <= 0.5;
        }

        private static string ParameterErrors(string? json)
        {
            return AnomalyParameters.TryParse(json, out _, out var errors) ? string.Empty : string.Join(" ", errors);
        }
    }
}
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
using Jube.Resources;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    internal sealed class AdvancedAnalyticsNeuralNetworkRegressorTrainingValidator
        : AbstractValidator<AdvancedAnalyticsNeuralNetworkRegressorTrainingDto>
    {
        public const int MaxNameLength = 256;
        public const int MaxRows = 1_000_000;
        public const int MaxVariables = 500;
        public const int MaxHiddenUnits = 128;
        public const int MaxEpochs = 5000;

        public AdvancedAnalyticsNeuralNetworkRegressorTrainingValidator(IStringLocalizer localiser,
            Func<int, CancellationToken, Task<bool>> instanceVisible)
        {
            RuleFor(p => p.AskJoobyAnalyticsInstanceId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.InstanceIdInvalid])
                .WithErrorCode("InstanceIdInvalid");

            RuleFor(p => p.AskJoobyAnalyticsInstanceId)
                .MustAsync((id, cancellation) => instanceVisible(id, cancellation))
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.InstanceNotFound])
                .WithErrorCode("InstanceNotFound")
                .When(p => p.AskJoobyAnalyticsInstanceId > 0);

            RuleFor(p => p.ModelName)
                .NotEmpty()
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.ModelNameRequired])
                .WithErrorCode("ModelNameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[AdvancedAnalyticsTrainingResources.ModelNameMaxLength], MaxNameLength))
                .WithErrorCode("ModelNameMaximumLength");

            RuleFor(p => p.CorrelationKey)
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(
                    localiser[AdvancedAnalyticsTrainingResources.CorrelationKeyMaxLength], MaxNameLength))
                .WithErrorCode("CorrelationKeyMaximumLength");

            RuleFor(p => p.VariableNames)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.VariablesRequired])
                .WithErrorCode("VariablesNotEmpty")
                .Must(names => names!.Length <= MaxVariables)
                .WithMessage(_ => string.Format(localiser[AdvancedAnalyticsTrainingResources.VariablesTooMany],
                    MaxVariables))
                .WithErrorCode("VariablesTooMany")
                .Must(names => names!.All(n => !string.IsNullOrWhiteSpace(n)))
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.VariableNameBlank])
                .WithErrorCode("VariableNameBlank")
                .Must(names => names!.Distinct(StringComparer.Ordinal).Count() == names!.Length)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.VariableNamesNotUnique])
                .WithErrorCode("VariableNamesNotUnique");

            RuleFor(p => p.ProcessingTypeIds)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.ProcessingTypeIdsRequired])
                .WithErrorCode("ProcessingTypeIdsRequired")
                .Must((dto, ids) => dto.VariableNames == null || ids!.Length == dto.VariableNames.Length)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.ProcessingTypeIdsMismatch])
                .WithErrorCode("ProcessingTypeIdsMismatch")
                .Must(ids => ids!.All(id => id is >= 1 and <= 7 or 99))
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.ProcessingTypeIdsInvalid])
                .WithErrorCode("ProcessingTypeIdsInvalid");

            RuleFor(p => p.Inputs)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.InputsRequired])
                .WithErrorCode("InputsNotEmpty")
                .Must(rows => rows!.Length <= MaxRows)
                .WithMessage(_ => string.Format(localiser[AdvancedAnalyticsTrainingResources.InputsTooManyRows],
                    MaxRows))
                .WithErrorCode("InputsTooManyRows")
                .Must((dto, rows) => dto.VariableNames == null || rows!.All(r => !ReferenceEquals(r, null)
                    && r.Length == dto.VariableNames.Length))
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.RowLengthMismatch])
                .WithErrorCode("RowLengthMismatch");

            RuleFor(p => p.Outputs)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.OutputsRequired])
                .WithErrorCode("OutputsNotNull")
                .Must((dto, outputs) => dto.Inputs == null || outputs!.Length == dto.Inputs.Length)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.OutputsLengthMismatch])
                .WithErrorCode("OutputsLengthMismatch")
                .Must(outputs => outputs!.All(double.IsFinite))
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.OutputsNotFinite])
                .WithErrorCode("OutputsNotFinite");

            RuleFor(p => p.HiddenUnits)
                .InclusiveBetween(1, MaxHiddenUnits)
                .WithMessage(_ => string.Format(localiser[AdvancedAnalyticsTrainingResources.HiddenUnitsInvalid],
                    MaxHiddenUnits))
                .WithErrorCode("HiddenUnitsInvalid");

            RuleFor(p => p.Epochs)
                .InclusiveBetween(1, MaxEpochs)
                .WithMessage(_ => string.Format(localiser[AdvancedAnalyticsTrainingResources.EpochsInvalid],
                    MaxEpochs))
                .WithErrorCode("EpochsInvalid");

            RuleFor(p => p.LearningRate)
                .Must(v => double.IsFinite(v) && v > 0)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.LearningRateInvalid])
                .WithErrorCode("LearningRateInvalid");

            RuleFor(p => p.ValidationFraction)
                .Must(Fraction)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.FractionRange])
                .WithErrorCode("ValidationFractionRange");

            RuleFor(p => p.TestFraction)
                .Must(Fraction)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.FractionRange])
                .WithErrorCode("TestFractionRange");

            RuleFor(p => p)
                .Must(p => p.ValidationFraction + p.TestFraction <= 0.9)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.FractionsTooLarge])
                .WithErrorCode("FractionsTooLarge")
                .When(p => Fraction(p.ValidationFraction) && Fraction(p.TestFraction));
        }

        private static bool Fraction(double value)
        {
            return double.IsFinite(value) && value >= 0 && value < 1;
        }
    }
}
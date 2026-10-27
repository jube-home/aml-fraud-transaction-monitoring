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
using Jube.Engine.AdvancedAnalytics.Training.Models;
using Jube.Resources;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingRun
{
    internal sealed class AdvancedAnalyticsAdaBoostClassifierTrainingValidator
        : AbstractValidator<AdvancedAnalyticsAdaBoostClassifierTrainingDto>
    {
        public const int MaxNameLength = 256;
        public const int MaxRows = 1_000_000;
        public const int MaxVariables = 500;
        public const int MaxRounds = 2000;
        public const int MaxDepthLimit = 32;
        public const int MaxLeafSize = 100_000;

        public AdvancedAnalyticsAdaBoostClassifierTrainingValidator(IStringLocalizer localiser,
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

            RuleFor(p => p.Input!.Kind)
                .IsInEnum()
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.InputKindInvalid])
                .WithErrorCode("InputKindInvalid")
                .When(p => p.Input != null);

            RuleFor(p => p.Input!.Pointer)
                .Must(pointer => pointer.HasValue && pointer.Value != Guid.Empty)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.CachePointerRequired])
                .WithErrorCode("CachePointerRequired")
                .When(p => p.Input is { Kind: AdvancedAnalyticsTrainingInputKind.CachePointer });

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
                .WithErrorCode("RowLengthMismatch")
                .When(IsInline);

            RuleFor(p => p.Labels)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.LabelsRequired])
                .WithErrorCode("LabelsRequired")
                .Must((dto, labels) => dto.Inputs == null || labels!.Length == dto.Inputs.Length)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.LabelsLengthMismatch])
                .WithErrorCode("LabelsLengthMismatch")
                .Must(labels => labels!.All(l => l is 0 or 1))
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.LabelsNotBinary])
                .WithErrorCode("LabelsNotBinary")
                .When(IsInline);

            RuleFor(p => p.Rounds)
                .InclusiveBetween(1, MaxRounds)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.TreeCountInvalid])
                .WithErrorCode("RoundsInvalid");

            RuleFor(p => p.MaxDepth)
                .InclusiveBetween(1, MaxDepthLimit)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.MaxDepthInvalid])
                .WithErrorCode("MaxDepthInvalid");

            RuleFor(p => p.MinimumLeafSize)
                .InclusiveBetween(1, MaxLeafSize)
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.MinLeafSizeInvalid])
                .WithErrorCode("MinimumLeafSizeInvalid");

            RuleFor(p => p.DecisionThreshold)
                .Must(v => v == null || (double.IsFinite(v.Value) && v.Value > 0 && v.Value < 1))
                .WithMessage(_ => localiser[AdvancedAnalyticsTrainingResources.DecisionThresholdInvalid])
                .WithErrorCode("DecisionThresholdInvalid");

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

        private static bool IsInline(AdvancedAnalyticsAdaBoostClassifierTrainingDto p)
        {
            return p.Input == null || p.Input.Kind == AdvancedAnalyticsTrainingInputKind.Inline;
        }

        private static bool Fraction(double value)
        {
            return double.IsFinite(value) && value >= 0 && value < 1;
        }
    }
}
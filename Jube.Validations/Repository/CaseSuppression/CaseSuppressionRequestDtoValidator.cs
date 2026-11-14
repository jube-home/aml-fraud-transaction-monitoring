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
using Jube.Dto.Repository.CaseSuppression;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseSuppression
{
    public sealed class CaseSuppressionRequestDtoValidator : AbstractValidator<CaseSuppressionRequestDto>
    {
        public const int MaxKeyLength = 256;
        public const int MaxValueLength = 256;
        public const int MaxReasonLength = 2000;
        public const int MaxTargets = 50;
        public const int MaxRuleNameLength = 256;
        public const int MaxArchiveIds = 500;
        public const int MaxMergeLength = 4000;
        public const int DefaultMaxDurationDays = 366;

        public CaseSuppressionRequestDtoValidator(IStringLocalizer localiser, int maxDurationDays = DefaultMaxDurationDays)
        {
            RuleFor(p => p.CaseId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseSuppressionResources.CaseIdRequired])
                .WithErrorCode("CaseIdGreaterThan");

            RuleFor(p => p.PathId)
                .InclusiveBetween(1, 2)
                .WithMessage(_ => localiser[CaseSuppressionResources.PathInvalid])
                .WithErrorCode("PathIdInclusiveBetween");

            RuleFor(p => p.OverrideKey)
                .Must(key => !string.IsNullOrWhiteSpace(key))
                .WithMessage(_ => localiser[CaseSuppressionResources.OverrideKeyRequired])
                .WithErrorCode("OverrideKeyNotEmpty");

            RuleFor(p => p.OverrideKey)
                .MaximumLength(MaxKeyLength)
                .WithMessage(_ => string.Format(localiser[CaseSuppressionResources.OverrideKeyMaxLength], MaxKeyLength))
                .WithErrorCode("OverrideKeyMaximumLength");

            RuleFor(p => p.OverrideKeyValue)
                .Must((dto, value) => dto.PathId != 2 || !string.IsNullOrEmpty(value))
                .WithMessage(_ => localiser[CaseSuppressionResources.OverrideKeyValueRequired])
                .WithErrorCode("OverrideKeyValueNotEmpty");

            RuleFor(p => p.OverrideKeyValue)
                .MaximumLength(MaxValueLength)
                .WithMessage(_ => string.Format(localiser[CaseSuppressionResources.OverrideKeyValueMaxLength],
                    MaxValueLength))
                .WithErrorCode("OverrideKeyValueMaximumLength");

            RuleFor(p => p.Reason)
                .Must(reason => !string.IsNullOrWhiteSpace(reason))
                .WithMessage(_ => localiser[CaseSuppressionResources.ReasonRequired])
                .WithErrorCode("ReasonNotEmpty");

            RuleFor(p => p.Reason)
                .MaximumLength(MaxReasonLength)
                .WithMessage(_ => string.Format(localiser[CaseSuppressionResources.ReasonMaxLength], MaxReasonLength))
                .WithErrorCode("ReasonMaximumLength");

            RuleFor(p => p.ExpiryDate)
                .Must(expiry => expiry.HasValue && expiry.Value > DateTimeOffset.UtcNow)
                .WithMessage(_ => localiser[CaseSuppressionResources.ExpiryRequired])
                .WithErrorCode("ExpiryDateFuture");

            RuleFor(p => p.ExpiryDate)
                .Must(expiry => !expiry.HasValue || expiry.Value <= DateTimeOffset.UtcNow.AddDays(maxDurationDays))
                .WithMessage(_ => string.Format(localiser[CaseSuppressionResources.ExpiryTooFar], maxDurationDays))
                .WithErrorCode("ExpiryDateTooFar");

            RuleFor(p => p.Targets)
                .Must(targets => targets is { Count: > 0 })
                .WithMessage(_ => localiser[CaseSuppressionResources.TargetsRequired])
                .WithErrorCode("TargetsNotEmpty");

            RuleFor(p => p.Targets)
                .Must(targets => targets is null || targets.Count <= MaxTargets)
                .WithMessage(_ => string.Format(localiser[CaseSuppressionResources.TargetsTooMany], MaxTargets))
                .WithErrorCode("TargetsMaximumCount");

            RuleForEach(p => p.Targets)
                .Must(target => target is not null && target.EntityAnalysisModelGuid != Guid.Empty)
                .WithMessage(_ => localiser[CaseSuppressionResources.TargetModelRequired])
                .WithErrorCode("TargetModelNotEmpty");

            RuleForEach(p => p.Targets)
                .Must(target => target is null || (target.RuleName ?? string.Empty).Length <= MaxRuleNameLength)
                .WithMessage(_ => string.Format(localiser[CaseSuppressionResources.RuleNameMaxLength],
                    MaxRuleNameLength))
                .WithErrorCode("RuleNameMaximumLength");

            RuleForEach(p => p.Targets)
                .Must(target => target is null || target.OverrideKindId is 0 or 1)
                .WithMessage(_ => localiser[CaseSuppressionResources.OverrideKindInvalid])
                .WithErrorCode("OverrideKindInvalid");

            RuleFor(p => p.ArchiveIds)
                .Must(ids => ids is null || ids.Count <= MaxArchiveIds)
                .WithMessage(_ => string.Format(localiser[CaseSuppressionResources.ArchiveIdsTooMany], MaxArchiveIds))
                .WithErrorCode("ArchiveIdsMaximumCount");

            RuleForEach(p => p.ArchiveIds)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseSuppressionResources.ArchiveIdInvalid])
                .WithErrorCode("ArchiveIdGreaterThan");

            RuleFor(p => p.Merge)
                .MaximumLength(MaxMergeLength)
                .WithMessage(_ => localiser[CaseSuppressionResources.MergeInvalid])
                .WithErrorCode("MergeMaximumLength");
        }
    }
}

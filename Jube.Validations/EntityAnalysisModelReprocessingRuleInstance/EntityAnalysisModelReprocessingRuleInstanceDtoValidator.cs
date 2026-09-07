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
using Jube.Dto.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelReprocessingRuleInstance
{
    public sealed class EntityAnalysisModelReprocessingRuleInstanceDtoValidator :
        AbstractValidator<EntityAnalysisModelReprocessingRuleInstanceDto>
    {
        private static readonly int[] allowedStatusIds = [0, 1, 2, 3, 4];

        public EntityAnalysisModelReprocessingRuleInstanceDtoValidator(
            EntityAnalysisModelReprocessingRuleRepository reprocessingRuleRepository, IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelReprocessingRuleId)
                .GreaterThan(0)
                .WithMessage(_ =>
                    localiser[
                        EntityAnalysisModelReprocessingRuleInstanceResources
                            .EntityAnalysisModelReprocessingRuleIdInvalid])
                .WithErrorCode("EntityAnalysisModelReprocessingRuleIdInvalid")
                .MustAsync(async (entityAnalysisModelReprocessingRuleId, cancellation) =>
                    await reprocessingRuleRepository
                        .GetByIdAsync(entityAnalysisModelReprocessingRuleId, cancellation) != null)
                .WithMessage(_ =>
                    localiser[
                        EntityAnalysisModelReprocessingRuleInstanceResources
                            .EntityAnalysisModelReprocessingRuleIdNotFound])
                .WithErrorCode("EntityAnalysisModelReprocessingRuleIdNotFound");

            RuleFor(p => p.StatusId)
                .Must(m => allowedStatusIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleInstanceResources.StatusIdInvalid])
                .WithErrorCode("StatusIdInvalid");

            RuleFor(p => p.AvailableCount)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleInstanceResources.AvailableCountRange])
                .WithErrorCode("AvailableCountRange");

            RuleFor(p => p.SampledCount)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleInstanceResources.SampledCountRange])
                .WithErrorCode("SampledCountRange");

            RuleFor(p => p.MatchedCount)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleInstanceResources.MatchedCountRange])
                .WithErrorCode("MatchedCountRange");

            RuleFor(p => p.ProcessedCount)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleInstanceResources.ProcessedCountRange])
                .WithErrorCode("ProcessedCountRange");

            RuleFor(p => p.ErrorCount)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleInstanceResources.ErrorCountRange])
                .WithErrorCode("ErrorCountRange");
        }
    }
}
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
using Jube.Dto.EntityAnalysisModelReprocessingRule;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelReprocessingRule
{
    public sealed class EntityAnalysisModelReprocessingRuleDtoValidator :
        AbstractValidator<EntityAnalysisModelReprocessingRuleDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxRuleScriptLength = 65536;

        private static readonly byte[] allowedRuleScriptTypeIds = [1, 2];
        private static readonly string[] allowedReprocessingIntervals = ["n", "h", "d", "m"];

        public EntityAnalysisModelReprocessingRuleDtoValidator(
            EntityAnalysisModelReprocessingRuleRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelReprocessingRuleResources.NameMaxLength],
                        MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Priority)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.PriorityRange])
                .WithErrorCode("PriorityRange");
            
            RuleFor(p => p.RuleScriptTypeId)
                .Must(m => allowedRuleScriptTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.RuleScriptTypeIdInvalid])
                .WithErrorCode("RuleScriptTypeIdInvalid");

            RuleFor(p => p.BuilderRuleScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.BuilderRuleScriptRequired])
                .WithErrorCode("BuilderRuleScriptNotEmpty")
                .MaximumLength(MaxRuleScriptLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelReprocessingRuleResources.BuilderRuleScriptMaxLength],
                        MaxRuleScriptLength))
                .WithErrorCode("BuilderRuleScriptMaximumLength")
                .When(p => p.RuleScriptTypeId == 1);

            RuleFor(p => p.Json)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.JsonRequired])
                .WithErrorCode("JsonNotEmpty")
                .When(p => p.RuleScriptTypeId == 1);

            RuleFor(p => p.CoderRuleScript)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.CoderRuleScriptRequired])
                .WithErrorCode("CoderRuleScriptNotEmpty")
                .MaximumLength(MaxRuleScriptLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelReprocessingRuleResources.CoderRuleScriptMaxLength],
                        MaxRuleScriptLength))
                .WithErrorCode("CoderRuleScriptMaximumLength")
                .When(p => p.RuleScriptTypeId == 2);

            RuleFor(p => p.ReprocessingSample)
                .InclusiveBetween(0, 100)
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.ReprocessingSampleRange])
                .WithErrorCode("ReprocessingSampleRange");

            RuleFor(p => p.ReprocessingValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.ReprocessingValueRange])
                .WithErrorCode("ReprocessingValueRange");

            RuleFor(p => p.ReprocessingInterval)
                .Must(m => allowedReprocessingIntervals.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelReprocessingRuleResources.ReprocessingIntervalInvalid])
                .WithErrorCode("ReprocessingIntervalInvalid");
        }
    }
}

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
using Jube.Dto.Repository.CaseWorkflow;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflow
{
    public sealed class CaseWorkflowDtoValidator : AbstractValidator<CaseWorkflowDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxDescriptionLength = 1024;

        public CaseWorkflowDtoValidator(CaseWorkflowRepository repository, IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[CaseWorkflowResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid")
                .MustAsync(async (entityAnalysisModelId, cancellation) =>
                    await repository.ExistsEntityAnalysisModelAsync(entityAnalysisModelId, cancellation))
                .WithMessage(_ => localiser[CaseWorkflowResources.EntityAnalysisModelNotFound])
                .WithErrorCode("EntityAnalysisModelIdNotFound");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => string.Format(localiser[CaseWorkflowResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name, dto.EntityAnalysisModelId, cancellation);
                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[CaseWorkflowResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Description)
                .MaximumLength(MaxDescriptionLength)
                .WithMessage(_ =>
                    string.Format(localiser[CaseWorkflowResources.DescriptionMaxLength], MaxDescriptionLength))
                .WithErrorCode("DescriptionMaximumLength");

            RuleFor(p => p.VisualisationRegistryGuid)
                .NotEmpty()
                .When(w => w.EnableVisualisation)
                .WithMessage(_ => localiser[CaseWorkflowResources.VisualisationRegistryGuidRequired])
                .WithErrorCode("VisualisationRegistryGuidNotEmpty")
                .MustAsync(async (visualisationRegistryGuid, cancellation) =>
                    await repository.ExistsVisualisationRegistryAsync(visualisationRegistryGuid, cancellation))
                .When(w => w.EnableVisualisation)
                .WithMessage(_ => localiser[CaseWorkflowResources.VisualisationRegistryNotFound])
                .WithErrorCode("VisualisationRegistryNotFound");
        }
    }
}
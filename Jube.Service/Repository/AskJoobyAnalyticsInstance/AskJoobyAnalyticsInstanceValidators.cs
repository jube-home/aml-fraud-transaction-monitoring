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
using Jube.Service.Repository.AskJoobyAnalyticsInstance.Models;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AskJoobyAnalyticsInstance
{
    internal sealed class AskJoobyAnalyticsInstanceDtoValidator : AbstractValidator<AskJoobyAnalyticsInstanceDto>
    {
        public const int MaxNameLength = 256;

        public AskJoobyAnalyticsInstanceDtoValidator(IStringLocalizer localiser,
            Func<string, int, CancellationToken, Task<bool>> nameTaken)
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage(_ => localiser[AskJoobyAnalyticsInstanceResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[AskJoobyAnalyticsInstanceResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength");

            RuleFor(p => p.Name)
                .MustAsync(async (dto, name, cancellation) =>
                    !await nameTaken(name!, dto.Id, cancellation).ConfigureAwait(false))
                .WithMessage(_ => localiser[AskJoobyAnalyticsInstanceResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate")
                .When(p => !string.IsNullOrWhiteSpace(p.Name) && p.Name.Length <= MaxNameLength);

            RuleFor(p => p.Active)
                .Must((dto, active) => !active || dto.EntityAnalysisModelId.HasValue)
                .WithMessage(_ => localiser[AskJoobyAnalyticsInstanceResources.ActiveRequiresAllocation])
                .WithErrorCode("ActiveRequiresAllocation");
        }
    }

    internal sealed record AskJoobyAnalyticsAllocationRequest(int InstanceId, int EntityAnalysisModelId);
}
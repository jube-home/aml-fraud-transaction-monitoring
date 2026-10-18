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
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsInstance
{
    internal sealed class AdvancedAnalyticsAllocationRequestValidator
        : AbstractValidator<AdvancedAnalyticsAllocationRequest>
    {
        public AdvancedAnalyticsAllocationRequestValidator(IStringLocalizer localiser,
            Func<int, CancellationToken, Task<bool>> modelVisible)
        {
            RuleFor(p => p.InstanceId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[AdvancedAnalyticsInstanceResources.InstanceIdInvalid])
                .WithErrorCode("InstanceIdInvalid");

            RuleFor(p => p.EntityAnalysisModelId)
                .GreaterThan(0)
                .WithMessage(_ => localiser[AdvancedAnalyticsInstanceResources.ModelInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.EntityAnalysisModelId)
                .MustAsync((id, cancellation) => modelVisible(id, cancellation))
                .WithMessage(_ => localiser[AdvancedAnalyticsInstanceResources.ModelNotFound])
                .WithErrorCode("EntityAnalysisModelIdNotFound")
                .When(p => p.EntityAnalysisModelId > 0);
        }
    }
}
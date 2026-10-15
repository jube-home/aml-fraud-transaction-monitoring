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
using Jube.Analytics.Datasets;
using Jube.Dto.Query.ExhaustiveSearchInstanceVariable;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.ExhaustiveSearchInstanceVariable
{
    public sealed class ExhaustiveSearchInstanceVariableReallocateDtoValidator
        : AbstractValidator<ExhaustiveSearchInstanceVariableReallocateDto>
    {
        public const int MaxNameLength = 64;

        public ExhaustiveSearchInstanceVariableReallocateDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.Name)
                .Must(DatasetMappingRules.IsSafeName)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceVariableResources.ReallocateNameInvalid])
                .WithErrorCode("ReallocateNameInvalid");

            RuleFor(p => p.ProcessingTypeId)
                .Must(DatasetProcessingTypes.IsKnown)
                .WithMessage(_ => localiser[ExhaustiveSearchInstanceVariableResources.ReallocateProcessingTypeInvalid])
                .WithErrorCode("ReallocateProcessingTypeInvalid");
        }
    }
}
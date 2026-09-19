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
using Jube.Dto.Query.EntityAnalysisModelSample;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Query.EntityAnalysisModelSample
{
    public class EntityAnalysisModelSampleOptionsDtoValidator : AbstractValidator<EntityAnalysisModelSampleOptionsDto>
    {
        public EntityAnalysisModelSampleOptionsDtoValidator(IStringLocalizer localiser)
        {
            RuleFor(p => p.EntityAnalysisModelGuid)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSampleResources.ModelRequired]);

            RuleFor(p => p.DateFrom)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSampleResources.DateFromRequired]);

            RuleFor(p => p.DateTo)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelSampleResources.DateToRequired])
                .GreaterThan(p => p.DateFrom)
                .WithMessage(_ => localiser[EntityAnalysisModelSampleResources.DateToAfterDateFrom]);

            RuleFor(p => p.Sample)
                .InclusiveBetween(0.0, 1.0)
                .WithMessage(_ => localiser[EntityAnalysisModelSampleResources.SampleRange]);
        }
    }
}
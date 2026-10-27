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

using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using Jube.Case;
using Jube.Case.Outbound;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelResponseElevationRange;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.EntityAnalysisModelResponseElevationRange
{
    public sealed partial class
        EntityAnalysisModelResponseElevationRangeDtoValidator : AbstractValidator<
            EntityAnalysisModelResponseElevationRangeDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxFreeTextLength = 65536;
        private const int MaxShortTextLength = 512;
        private static readonly int[] httpEndpointTypeIds = [1, 2];
        private static readonly int[] notificationTypeIds = [1, 2];

        public EntityAnalysisModelResponseElevationRangeDtoValidator(
            EntityAnalysisModelResponseElevationRangeRepository repository, IStringLocalizer localiser,
            EntityAnalysisModelRepository entityAnalysisModelRepository, OutboundHostPolicy policy)
        {
            RuleFor(p => p.EntityAnalysisModelId)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid")
                .MustAsync(async (id, cancellation) =>
                    await entityAnalysisModelRepository.GetByIdAsync(id, cancellation) != null)
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.EntityAnalysisModelIdInvalid])
                .WithErrorCode("EntityAnalysisModelIdInvalid");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelResponseElevationRangeResources.NameMaxLength], MaxNameLength))
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository
                        .GetByNameEntityAnalysisModelIdAsync(name!, dto.EntityAnalysisModelId, cancellation);

                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.NameAlreadyExists])
                .WithErrorCode("NameDuplicate");

            RuleFor(p => p.Minimum)
                .GreaterThanOrEqualTo(0)
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.MinimumRange])
                .WithErrorCode("MinimumRange");

            RuleFor(p => p.Maximum)
                .GreaterThanOrEqualTo(p => p.Minimum)
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.MaximumRange])
                .WithErrorCode("MaximumRange");

            RuleFor(p => p)
                .CustomAsync(async (dto, context, cancellation) =>
                {
                    var overlapping = await repository.GetOverlappingAsync(dto.EntityAnalysisModelId, dto.Minimum,
                        dto.Maximum, dto.Id, cancellation);

                    if (overlapping != null)
                    {
                        context.AddFailure(new ValidationFailure(
                            nameof(EntityAnalysisModelResponseElevationRangeDto.Minimum),
                            string.Format(localiser[EntityAnalysisModelResponseElevationRangeResources.RangeOverlaps],
                                overlapping.Name))
                        {
                            ErrorCode = "RangeOverlaps"
                        });
                    }
                })
                .When(w => w.EntityAnalysisModelId > 0 && w.Minimum >= 0 && w.Maximum >= w.Minimum);

            RuleFor(p => p.Hold)
                .Equal(false)
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.DeclineAndHold])
                .WithErrorCode("DeclineAndHold")
                .When(w => w.Decline);

            RuleFor(p => p.Content)
                .MaximumLength(MaxFreeTextLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelResponseElevationRangeResources.ContentMaxLength], MaxFreeTextLength))
                .WithErrorCode("ContentMaximumLength");

            RuleFor(p => p.Redirect)
                .MaximumLength(MaxShortTextLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelResponseElevationRangeResources.RedirectMaxLength], MaxShortTextLength))
                .WithErrorCode("RedirectMaximumLength");

            RuleFor(p => p.ForeColor)
                .Must(m => HexColor().IsMatch(m!))
                .When(w => !string.IsNullOrEmpty(w.ForeColor))
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.ColourInvalid])
                .WithErrorCode("ForeColorInvalid");

            RuleFor(p => p.BackColor)
                .Must(m => HexColor().IsMatch(m!))
                .When(w => !string.IsNullOrEmpty(w.BackColor))
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.ColourInvalid])
                .WithErrorCode("BackColorInvalid");

            RuleFor(p => p.HttpEndpoint)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.HttpEndpointRequired])
                .WithErrorCode("HttpEndpointNotEmpty")
                .MaximumLength(MaxShortTextLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelResponseElevationRangeResources.HttpEndpointMaxLength], MaxShortTextLength))
                .WithErrorCode("HttpEndpointMaximumLength")
                .Must(m => WebhookTarget.Resolve(m, ProbeTokens).Refusal == null)
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.HttpEndpointInvalid])
                .WithErrorCode("HttpEndpointInvalid")
                .Must(m => policy.CheckTemplate(m) == null)
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.HttpEndpointHostBlocked])
                .WithErrorCode("HttpEndpointHostBlocked")
                .When(w => w.EnableHttpEndpoint);

            RuleFor(p => p.HttpEndpointTypeId)
                .Must(m => httpEndpointTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.HttpEndpointTypeIdInvalid])
                .WithErrorCode("HttpEndpointTypeIdInvalid")
                .When(w => w.EnableHttpEndpoint);

            RuleFor(p => p.HttpEndpointHeaderName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.HttpEndpointHeaderRequired])
                .WithErrorCode("HttpEndpointHeaderNameNotEmpty")
                .Must(HookCredentials.IsAllowedHeaderName)
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.HttpEndpointHeaderInvalid])
                .WithErrorCode("HttpEndpointHeaderNameInvalid")
                .When(w => w.EnableHttpEndpoint && !string.IsNullOrEmpty(w.HttpEndpointCredentialVariable));

            RuleFor(p => p.HttpEndpointCredentialVariable)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.HttpEndpointCredentialRequired])
                .WithErrorCode("HttpEndpointCredentialVariableNotEmpty")
                .Must(HookCredentials.IsAllowedSecretName)
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.HttpEndpointCredentialInvalid])
                .WithErrorCode("HttpEndpointCredentialVariableInvalid")
                .When(w => w.EnableHttpEndpoint && !string.IsNullOrEmpty(w.HttpEndpointHeaderName));

            RuleFor(p => p.NotificationTypeId)
                .Must(m => notificationTypeIds.Contains(m))
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.NotificationTypeIdInvalid])
                .WithErrorCode("NotificationTypeIdInvalid")
                .When(w => w.EnableNotification);

            RuleFor(p => p.NotificationDestination)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[EntityAnalysisModelResponseElevationRangeResources.NotificationDestinationRequired])
                .WithErrorCode("NotificationDestinationNotEmpty")
                .MaximumLength(MaxShortTextLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelResponseElevationRangeResources.NotificationDestinationMaxLength], MaxShortTextLength))
                .WithErrorCode("NotificationDestinationMaximumLength")
                .When(w => w.EnableNotification);

            RuleFor(p => p.NotificationSubject)
                .MaximumLength(MaxShortTextLength)
                .WithMessage(_ =>
                    string.Format(localiser[EntityAnalysisModelResponseElevationRangeResources.NotificationSubjectMaxLength], MaxShortTextLength))
                .WithErrorCode("NotificationSubjectMaximumLength");

            RuleFor(p => p.NotificationBody)
                .MaximumLength(MaxFreeTextLength)
                .WithMessage(_ => string.Format(localiser[EntityAnalysisModelResponseElevationRangeResources.NotificationBodyMaxLength], MaxFreeTextLength))
                .WithErrorCode("NotificationBodyMaximumLength");
        }

        private static string ProbeTokens(string template)
        {
            return TokenPattern().Replace(template, "x");
        }

        [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
        private static partial Regex HexColor();

        [GeneratedRegex(@"\[@.*?@\]")]
        private static partial Regex TokenPattern();
    }
}

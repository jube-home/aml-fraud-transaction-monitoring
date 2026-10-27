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
using Jube.Case;
using Jube.Case.Outbound;
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseWorkflowReleaseHook;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Validations.Repository.CaseWorkflowReleaseHook
{
    public sealed partial class CaseWorkflowReleaseHookDtoValidator : AbstractValidator<CaseWorkflowReleaseHookDto>
    {
        private const int MaxNameLength = 256;
        private const int MaxSourceValueLength = 200;
        private const int MaxBodyLength = 8000;
        private static readonly int[] httpEndpointTypeIds = [1, 2];

        private static readonly string[] UrlTokens = ["Hold.Name", "Hold.Elevation", "Hold.EntryGuid"];
        private static readonly string[] BodyTokens = ["Decision.Action", "Decision.Reason", "Decision.User"];

        public CaseWorkflowReleaseHookDtoValidator(CaseWorkflowReleaseHookRepository repository,
            IStringLocalizer localiser, OutboundHostPolicy policy)
        {
            RuleFor(p => p.CaseWorkflowId)
                .Cascade(CascadeMode.Stop)
                .MustAsync(async (id, cancellation) =>
                    id > 0 && await repository.GetCaseWorkflowByIdAsync(id, cancellation) != null)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.CaseWorkflowNotFound])
                .WithErrorCode("CaseWorkflowIdInvalid");

            RuleFor(p => p.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.NameRequired])
                .WithErrorCode("NameNotEmpty")
                .MaximumLength(MaxNameLength)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.NameTooLong])
                .WithErrorCode("NameMaximumLength")
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameAsync(name!, dto.CaseWorkflowId, cancellation);

                    return existing == null || existing.Id == dto.Id;
                })
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.NameAlreadyExists])
                .WithErrorCode("NameAlreadyExists");

            RuleFor(p => p.SourceField)
                .Must(m => SourceFieldPattern().IsMatch(m!))
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.SourceFieldInvalid])
                .WithErrorCode("SourceFieldInvalid")
                .When(w => !string.IsNullOrWhiteSpace(w.SourceField));

            RuleFor(p => p.SourceValue)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.SourceValueRequired])
                .WithErrorCode("SourceValueNotEmpty")
                .MaximumLength(MaxSourceValueLength)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.SourceValueTooLong])
                .WithErrorCode("SourceValueMaximumLength")
                .When(w => !string.IsNullOrWhiteSpace(w.SourceField));

            RuleFor(p => p.SourceValue)
                .Must(string.IsNullOrWhiteSpace)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.SourceValueWithoutField])
                .WithErrorCode("SourceValueWithoutField")
                .When(w => string.IsNullOrWhiteSpace(w.SourceField));

            RuleFor(p => p.ReleaseUrl)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.ReleaseUrlRequired])
                .WithErrorCode("ReleaseUrlNotEmpty")
                .Must(m => UrlAllowed(m!))
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.ReleaseUrlInvalid])
                .WithErrorCode("ReleaseUrlInvalid")
                .Must(m => policy.CheckTemplate(m) == null)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.ReleaseUrlHostBlocked])
                .WithErrorCode("ReleaseUrlHostBlocked");

            RuleFor(p => p.RefuseUrl)
                .Must(m => UrlAllowed(m!))
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.RefuseUrlInvalid])
                .WithErrorCode("RefuseUrlInvalid")
                .Must(m => policy.CheckTemplate(m) == null)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.RefuseUrlHostBlocked])
                .WithErrorCode("RefuseUrlHostBlocked")
                .When(w => !string.IsNullOrWhiteSpace(w.RefuseUrl));

            RuleFor(p => p)
                .Custom((dto, context) =>
                {
                    foreach (var url in new[] { dto.ReleaseUrl, dto.RefuseUrl })
                    {
                        foreach (var token in Unknown(url, UrlTokens))
                        {
                            context.AddFailure(nameof(dto.ReleaseUrl),
                                string.Format(localiser[CaseWorkflowReleaseHookResources.TokenUnknown], token));
                        }
                    }

                    foreach (var token in Unknown(dto.Body, [.. UrlTokens, .. BodyTokens]))
                    {
                        context.AddFailure(nameof(dto.Body),
                            string.Format(localiser[CaseWorkflowReleaseHookResources.TokenUnknown], token));
                    }
                });

            RuleFor(p => p.HttpEndpointTypeId)
                .Must(m => httpEndpointTypeIds.Contains(m))
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.HttpEndpointTypeIdInvalid])
                .WithErrorCode("HttpEndpointTypeIdInvalid");

            RuleFor(p => p.Body)
                .Cascade(CascadeMode.Stop)
                .MaximumLength(MaxBodyLength)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.BodyTooLong])
                .WithErrorCode("BodyMaximumLength")
                .Must(string.IsNullOrWhiteSpace)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.BodyOnlyForPost])
                .WithErrorCode("BodyOnlyForPost")
                .When(w => w.HttpEndpointTypeId == 2);

            RuleFor(p => p.HeaderName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.HeaderRequired])
                .WithErrorCode("HeaderNameNotEmpty")
                .Must(HookCredentials.IsAllowedHeaderName)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.HeaderInvalid])
                .WithErrorCode("HeaderNameInvalid")
                .When(w => !string.IsNullOrEmpty(w.CredentialVariable));

            RuleFor(p => p.HeaderName)
                .Must(HookCredentials.IsAllowedHeaderName)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.HeaderInvalid])
                .WithErrorCode("HeaderNameInvalid")
                .When(w => !string.IsNullOrEmpty(w.HeaderName) && string.IsNullOrEmpty(w.CredentialVariable));

            RuleFor(p => p.CredentialVariable)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.CredentialRequired])
                .WithErrorCode("CredentialVariableNotEmpty")
                .Must(HookCredentials.IsAllowedSecretName)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.CredentialInvalid])
                .WithErrorCode("CredentialVariableInvalid")
                .When(w => !string.IsNullOrEmpty(w.HeaderName));

            RuleFor(p => p.CredentialVariable)
                .Must(HookCredentials.IsAllowedSecretName)
                .WithMessage(_ => localiser[CaseWorkflowReleaseHookResources.CredentialInvalid])
                .WithErrorCode("CredentialVariableInvalid")
                .When(w => !string.IsNullOrEmpty(w.CredentialVariable) && string.IsNullOrEmpty(w.HeaderName));
        }

        private static bool UrlAllowed(string url)
        {
            return WebhookTarget.Resolve(url, template => TokenPattern().Replace(template, "x")).Refusal == null
                   && !Unknown(url, UrlTokens).Any();
        }

        private static IEnumerable<string> Unknown(string? template, IEnumerable<string> allowed)
        {
            if (string.IsNullOrEmpty(template))
            {
                return [];
            }

            return TokenPattern().Matches(template)
                .Select(s => s.Groups[1].Value)
                .Where(w => !allowed.Contains(w, StringComparer.OrdinalIgnoreCase)
                            && !w.StartsWith("Payload.", StringComparison.OrdinalIgnoreCase))
                .Distinct();
        }

        [GeneratedRegex(@"\[@(.*?)@\]")]
        private static partial Regex TokenPattern();

        [GeneratedRegex(@"^[A-Za-z0-9_][A-Za-z0-9_ .:-]{0,127}$")]
        private static partial Regex SourceFieldPattern();
    }
}

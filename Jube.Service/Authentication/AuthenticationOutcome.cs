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

using FluentValidation.Results;
using Jube.Dto.Authentication;

namespace Jube.Service.Authentication
{
    public sealed record AuthenticationOutcome(
        AuthenticationOutcomeKind Kind,
        AuthenticationResponseDto? Response = null,
        ValidationResult? Validation = null,
        IReadOnlyList<string>? Errors = null,
        string? Message = null,
        bool? WirePasswordHash = null)
    {
        public static AuthenticationOutcome WireScheme(bool wirePasswordHash) =>
            new(AuthenticationOutcomeKind.Ok, WirePasswordHash: wirePasswordHash);

        public static AuthenticationOutcome Ok(AuthenticationResponseDto response) =>
            new(AuthenticationOutcomeKind.Ok, response);

        public static AuthenticationOutcome BadRequest() => new(AuthenticationOutcomeKind.BadRequest);
        public static AuthenticationOutcome Accepted() => new(AuthenticationOutcomeKind.Accepted);
        public static AuthenticationOutcome Unauthorized() => new(AuthenticationOutcomeKind.Unauthorized);
        public static AuthenticationOutcome Forbidden() => new(AuthenticationOutcomeKind.Forbidden);
        public static AuthenticationOutcome NotFound() => new(AuthenticationOutcomeKind.NotFound);

        public static AuthenticationOutcome ValidationFailed(ValidationResult validation) =>
            new(AuthenticationOutcomeKind.ValidationFailed, Validation: validation);

        public static AuthenticationOutcome PasswordStrength(IReadOnlyList<string> errors) =>
            new(AuthenticationOutcomeKind.PasswordStrength, Errors: errors);

        public static AuthenticationOutcome BadRequestMessage(string message) =>
            new(AuthenticationOutcomeKind.BadRequestMessage, Message: message);
    }
}
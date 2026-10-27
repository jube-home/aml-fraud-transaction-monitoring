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

namespace Jube.Resources
{
    public sealed class CaseWorkflowReleaseHookResources
    {
        public const string PermissionDenied = nameof(PermissionDenied);
        public const string NotAuthenticated = nameof(NotAuthenticated);
        public const string NotFound = nameof(NotFound);
        public const string CaseWorkflowNotFound = nameof(CaseWorkflowNotFound);
        public const string NameRequired = nameof(NameRequired);
        public const string NameTooLong = nameof(NameTooLong);
        public const string NameAlreadyExists = nameof(NameAlreadyExists);
        public const string SourceFieldInvalid = nameof(SourceFieldInvalid);
        public const string SourceValueRequired = nameof(SourceValueRequired);
        public const string SourceValueWithoutField = nameof(SourceValueWithoutField);
        public const string SourceValueTooLong = nameof(SourceValueTooLong);
        public const string ReleaseUrlRequired = nameof(ReleaseUrlRequired);
        public const string ReleaseUrlInvalid = nameof(ReleaseUrlInvalid);
        public const string RefuseUrlInvalid = nameof(RefuseUrlInvalid);
        public const string ReleaseUrlHostBlocked = nameof(ReleaseUrlHostBlocked);
        public const string RefuseUrlHostBlocked = nameof(RefuseUrlHostBlocked);
        public const string HttpEndpointTypeIdInvalid = nameof(HttpEndpointTypeIdInvalid);
        public const string HeaderRequired = nameof(HeaderRequired);
        public const string HeaderInvalid = nameof(HeaderInvalid);
        public const string CredentialRequired = nameof(CredentialRequired);
        public const string CredentialInvalid = nameof(CredentialInvalid);
        public const string BodyTooLong = nameof(BodyTooLong);
        public const string BodyOnlyForPost = nameof(BodyOnlyForPost);
        public const string TokenUnknown = nameof(TokenUnknown);
    }
}

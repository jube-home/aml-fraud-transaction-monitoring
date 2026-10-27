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
    public sealed class CaseHoldResources
    {
        public const string PermissionDenied = nameof(PermissionDenied);
        public const string NotAuthenticated = nameof(NotAuthenticated);
        public const string NotFound = nameof(NotFound);
        public const string CaseIdInvalid = nameof(CaseIdInvalid);
        public const string EntryRequired = nameof(EntryRequired);
        public const string ReasonRequired = nameof(ReasonRequired);
        public const string ReasonTooLong = nameof(ReasonTooLong);
        public const string NotHeld = nameof(NotHeld);
        public const string CaseLocked = nameof(CaseLocked);
        public const string NoReleaseHook = nameof(NoReleaseHook);
        public const string HookRefused = nameof(HookRefused);
        public const string CredentialMissing = nameof(CredentialMissing);
        public const string HookFailed = nameof(HookFailed);
        public const string HookBlockedByPolicy = nameof(HookBlockedByPolicy);
        public const string HookStatusNotOk = nameof(HookStatusNotOk);
    }
}

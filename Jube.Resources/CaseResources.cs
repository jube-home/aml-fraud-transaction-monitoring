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
    public sealed class CaseResources
    {
        public const string PermissionDenied = nameof(PermissionDenied);
        public const string NotAuthenticated = nameof(NotAuthenticated);
        public const string NotFound = nameof(NotFound);
        public const string CaseWorkflowNotFound = nameof(CaseWorkflowNotFound);
        public const string ArchiveNotFound = nameof(ArchiveNotFound);
        public const string InvalidCaseWorkflowStatus = nameof(InvalidCaseWorkflowStatus);
        public const string CaseAlreadyExists = nameof(CaseAlreadyExists);
        public const string CaseCreationFailed = nameof(CaseCreationFailed);
        public const string IdRequired = nameof(IdRequired);
        public const string LockedUserRequired = nameof(LockedUserRequired);
        public const string CaseWorkflowStatusGuidRequired = nameof(CaseWorkflowStatusGuidRequired);
        public const string DiaryDateRequired = nameof(DiaryDateRequired);
        public const string RatingRequired = nameof(RatingRequired);
        public const string PayloadRequired = nameof(PayloadRequired);
        public const string CaseWorkflowGuidRequired = nameof(CaseWorkflowGuidRequired);
        public const string CaseKeyRequired = nameof(CaseKeyRequired);
        public const string CaseKeyValueRequired = nameof(CaseKeyValueRequired);
        public const string LockedUserInvalid = nameof(LockedUserInvalid);
        public const string LockedByAnotherUser = nameof(LockedByAnotherUser);
    }
}
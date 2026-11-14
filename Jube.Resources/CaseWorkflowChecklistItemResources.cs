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
    public sealed class CaseWorkflowChecklistItemResources
    {
        public const string PermissionDenied = nameof(PermissionDenied);
        public const string SqlPermissionDenied = nameof(SqlPermissionDenied);
        public const string NotAuthenticated = nameof(NotAuthenticated);
        public const string NotFound = nameof(NotFound);
        public const string CaseWorkflowChecklistGuidRequired = nameof(CaseWorkflowChecklistGuidRequired);
        public const string CaseWorkflowChecklistNotFound = nameof(CaseWorkflowChecklistNotFound);
        public const string NameRequired = nameof(NameRequired);
        public const string NameTooLong = nameof(NameTooLong);
        public const string PriorityInvalid = nameof(PriorityInvalid);
        public const string SlaMinutesInvalid = nameof(SlaMinutesInvalid);
        public const string ValidateMessageTooLong = nameof(ValidateMessageTooLong);
        public const string SqlTooLong = nameof(SqlTooLong);
        public const string ShowSqlRequired = nameof(ShowSqlRequired);
        public const string ValidateSqlRequired = nameof(ValidateSqlRequired);
        public const string SqlEnableNeedsPermission = nameof(SqlEnableNeedsPermission);
        public const string SqlRejected = nameof(SqlRejected);
        public const string SqlParameterUnknown = nameof(SqlParameterUnknown);
        public const string ConditionInvalid = nameof(ConditionInvalid);
        public const string SqlNotPassed = nameof(SqlNotPassed);
        public const string SqlFailed = nameof(SqlFailed);
    }
}

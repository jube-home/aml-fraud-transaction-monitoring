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
    public sealed class EntityAnalysisModelSuppressionResources
    {
        public const string PermissionDenied = nameof(PermissionDenied);
        public const string NotAuthenticated = nameof(NotAuthenticated);
        public const string SuppressionKeyRequired = nameof(SuppressionKeyRequired);
        public const string SuppressionKeyMaxLength = nameof(SuppressionKeyMaxLength);
        public const string SuppressionKeyValueRequired = nameof(SuppressionKeyValueRequired);
        public const string SuppressionKeyValueMaxLength = nameof(SuppressionKeyValueMaxLength);
        public const string EntityAnalysisModelGuidRequired = nameof(EntityAnalysisModelGuidRequired);
        public const string EntityAnalysisModelGuidNotFound = nameof(EntityAnalysisModelGuidNotFound);
        public const string DeleteExpiryDateMustBeFuture = nameof(DeleteExpiryDateMustBeFuture);
    }
}
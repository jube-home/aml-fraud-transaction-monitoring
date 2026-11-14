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
    public sealed class GraphRegistryEdgeResources
    {
        public const string PermissionDenied = nameof(PermissionDenied);
        public const string NotAuthenticated = nameof(NotAuthenticated);
        public const string NotFound = nameof(NotFound);
        public const string GraphRegistryNotFound = nameof(GraphRegistryNotFound);
        public const string NameRequired = nameof(NameRequired);
        public const string NameMaxLength = nameof(NameMaxLength);
        public const string NameAlreadyExists = nameof(NameAlreadyExists);
        public const string KindInvalid = nameof(KindInvalid);
        public const string FromKeyRequired = nameof(FromKeyRequired);
        public const string ToKeyRequired = nameof(ToKeyRequired);
        public const string KeysMustDiffer = nameof(KeysMustDiffer);
        public const string KeyMaxLength = nameof(KeyMaxLength);
        public const string AmountKeyRequired = nameof(AmountKeyRequired);
        public const string HalfLifeOutOfRange = nameof(HalfLifeOutOfRange);
        public const string KindPriorOutOfRange = nameof(KindPriorOutOfRange);
        public const string HubThresholdOutOfRange = nameof(HubThresholdOutOfRange);
        public const string MinimumAmountNegative = nameof(MinimumAmountNegative);
    }
}

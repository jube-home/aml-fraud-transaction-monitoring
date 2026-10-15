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

namespace Jube.Service.Repository.AdvancedAnalyticsOperation
{
    public static class AdvancedAnalyticsPermissionSpecifications
    {
        public const int Analytics = 48;
        public const int Training = 66;
        public const int Recall = 46;
        public const int PromoteModel = 47;
        internal static readonly int[] analyticsOnly = [Analytics];
        internal static readonly int[] trainingOnly = [Training];
        internal static readonly int[] promoteModelOnly = [PromoteModel];
    }
}
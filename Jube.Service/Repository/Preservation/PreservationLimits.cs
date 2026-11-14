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

namespace Jube.Service.Repository.Preservation
{
    public sealed record PreservationLimits(
        long MaximumPackageBytes,
        long MaximumDecompressedBytes,
        int MaximumSnapshotsPerTenant,
        int MaximumSnapshotAgeDays)
    {
        public static PreservationLimits Default { get; } = new(104_857_600, 1_073_741_824, 200, 365);

        public static PreservationLimits FromEnvironment(DynamicEnvironment.DynamicEnvironment environment)
        {
            return new PreservationLimits(
                Read(environment, "PreservationImportMaxBytes", Default.MaximumPackageBytes, false),
                Read(environment, "PreservationImportMaxDecompressedBytes", Default.MaximumDecompressedBytes, false),
                (int)Math.Min(Read(environment, "PreservationSnapshotMaxPerTenant", Default.MaximumSnapshotsPerTenant,
                    true), int.MaxValue),
                (int)Math.Min(Read(environment, "PreservationSnapshotMaxAgeDays", Default.MaximumSnapshotAgeDays,
                    true), int.MaxValue));
        }

        private static long Read(DynamicEnvironment.DynamicEnvironment environment, string key, long fallback,
            bool zeroDisables)
        {
            return long.TryParse(environment.AppSettings(key), out var value) && (value > 0 || zeroDisables && value == 0)
                ? value
                : fallback;
        }
    }
}

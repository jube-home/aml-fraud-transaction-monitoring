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

using Jube.Analytics.Datasets.Models;

namespace Jube.Service.Repository.ExhaustiveInstanceDataset
{
    public static class DatasetLimitsFactory
    {
        public static DatasetLimits FromEnvironment(DynamicEnvironment.DynamicEnvironment environment)
        {
            var defaults = DatasetLimits.Default;

            return defaults with
            {
                MaxBytes = ReadLong(environment, "ExhaustiveDatasetUploadMaxBytes", defaults.MaxBytes),
                MaxRows = (int)Math.Min(ReadLong(environment, "ExhaustiveDatasetUploadMaxRows", defaults.MaxRows),
                    int.MaxValue),
                MaxColumns = (int)Math.Min(
                    ReadLong(environment, "ExhaustiveDatasetUploadMaxColumns", defaults.MaxColumns), 100000)
            };
        }

        private static long ReadLong(DynamicEnvironment.DynamicEnvironment environment, string key, long fallback)
        {
            var text = environment.AppSettings(key);

            return long.TryParse(text, out var value) && value > 0 ? value : fallback;
        }
    }
}
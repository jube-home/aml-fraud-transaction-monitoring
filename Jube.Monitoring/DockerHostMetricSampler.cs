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

using Jube.Data.Poco;
using Newtonsoft.Json.Linq;

namespace Jube.Monitoring
{
    public sealed class DockerHostMetricSampler(DockerApiClient client)
    {
        public async Task<DockerHostMetric?> SampleAsync()
        {
            var info = await client.GetInfoAsync().ConfigureAwait(false);
            if (info == null)
            {
                return null;
            }

            var version = await client.GetVersionAsync().ConfigureAwait(false);
            var systemDf = await client.GetSystemDfAsync().ConfigureAwait(false);

            return new DockerHostMetric
            {
                ContainersTotal = (int?)info["Containers"],
                ContainersRunning = (int?)info["ContainersRunning"],
                ContainersPaused = (int?)info["ContainersPaused"],
                ContainersStopped = (int?)info["ContainersStopped"],
                ImagesCount = (int?)info["Images"],
                NCpu = (int?)info["NCPU"],
                MemTotalBytes = (long?)info["MemTotal"],
                DockerVersion = (string?)version?["Version"],
                ApiVersion = (string?)version?["ApiVersion"],
                KernelVersion = (string?)info["KernelVersion"],
                OperatingSystem = (string?)info["OperatingSystem"],
                OsType = (string?)info["OSType"],
                Architecture = (string?)info["Architecture"],
                LayersSizeBytes = (long?)systemDf?["LayersSize"],
                ImagesSizeBytes = SumField(systemDf?["Images"], "Size"),
                ReclaimableImagesBytes = SumReclaimableImages(systemDf?["Images"]),
                ContainersDiskBytes = SumField(systemDf?["Containers"], "SizeRw"),
                VolumesSizeBytes = SumField(systemDf?["Volumes"], "UsageData", "Size"),
                BuildCacheSizeBytes = SumField(systemDf?["BuildCache"], "Size")
            };
        }

        private static long? SumField(JToken? array, string field)
        {
            if (array is not JArray items)
            {
                return null;
            }

            long total = 0;
            foreach (var item in items)
            {
                var value = (long?)item[field];
                if (value.HasValue)
                {
                    total += value.Value;
                }
            }

            return total;
        }

        private static long? SumField(JToken? array, string parentField, string field)
        {
            if (array is not JArray items)
            {
                return null;
            }

            long total = 0;
            foreach (var item in items)
            {
                var value = (long?)item[parentField]?[field];
                if (value.HasValue)
                {
                    total += value.Value;
                }
            }

            return total;
        }

        private static long? SumReclaimableImages(JToken? array)
        {
            if (array is not JArray items)
            {
                return null;
            }

            long total = 0;
            foreach (var item in items)
            {
                var containers = (long?)item["Containers"];
                var size = (long?)item["Size"];
                if (containers == 0 && size.HasValue)
                {
                    total += size.Value;
                }
            }

            return total;
        }
    }
}
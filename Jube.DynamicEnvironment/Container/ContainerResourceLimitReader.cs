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

using System;
using System.Globalization;
using System.IO;
using Jube.DynamicEnvironment.Container.Models;

namespace Jube.DynamicEnvironment.Container
{
    public static class ContainerResourceLimitReader
    {
        private const string CgroupRoot = "/sys/fs/cgroup";

        private static readonly ContainerResourceLimits empty = new(null, null, null, null, null, null);

        public static ContainerResourceLimits Read()
        {
            try
            {
                var v2 = ReadCgroupV2();
                if (v2 != null)
                {
                    return v2;
                }
            }
            catch
            {
                //Ignore
            }

            try
            {
                return ReadCgroupV1() ?? empty;
            }
            catch
            {
                return empty;
            }
        }

        private static ContainerResourceLimits ReadCgroupV2()
        {
            var cpuMaxPath = Path.Combine(CgroupRoot, "cpu.max");
            var cpuStatPath = Path.Combine(CgroupRoot, "cpu.stat");
            var memoryMaxPath = Path.Combine(CgroupRoot, "memory.max");
            var memoryCurrentPath = Path.Combine(CgroupRoot, "memory.current");

            if (!File.Exists(cpuMaxPath) && !File.Exists(memoryMaxPath))
            {
                return null;
            }

            double? cpuLimitCores = null;
            if (File.Exists(cpuMaxPath))
            {
                var parts = File.ReadAllText(cpuMaxPath).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[0] != "max" &&
                    double.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quota) &&
                    double.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var period) &&
                    period > 0)
                {
                    cpuLimitCores = quota / period;
                }
            }

            long? cpuUsageMicroseconds = null;
            long? cpuThrottledPeriods = null;
            long? cpuThrottledMicroseconds = null;
            if (File.Exists(cpuStatPath))
            {
                foreach (var line in File.ReadAllLines(cpuStatPath))
                {
                    var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length != 2 || !long.TryParse(fields[1], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out var value))
                    {
                        continue;
                    }

                    switch (fields[0])
                    {
                        case "usage_usec":
                            cpuUsageMicroseconds = value;
                            break;
                        case "nr_throttled":
                            cpuThrottledPeriods = value;
                            break;
                        case "throttled_usec":
                            cpuThrottledMicroseconds = value;
                            break;
                    }
                }
            }

            long? memoryLimitBytes = null;
            if (File.Exists(memoryMaxPath))
            {
                var raw = File.ReadAllText(memoryMaxPath).Trim();
                if (raw != "max" &&
                    long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var limit))
                {
                    memoryLimitBytes = limit;
                }
            }

            long? memoryUsageBytes = null;
            if (File.Exists(memoryCurrentPath) &&
                long.TryParse(File.ReadAllText(memoryCurrentPath).Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var usage))
            {
                memoryUsageBytes = usage;
            }

            return new ContainerResourceLimits(cpuLimitCores, cpuUsageMicroseconds, cpuThrottledPeriods,
                cpuThrottledMicroseconds, memoryLimitBytes, memoryUsageBytes);
        }

        private static ContainerResourceLimits ReadCgroupV1()
        {
            var quotaPath = Path.Combine(CgroupRoot, "cpu", "cpu.cfs_quota_us");
            var periodPath = Path.Combine(CgroupRoot, "cpu", "cpu.cfs_period_us");
            var cpuStatPath = Path.Combine(CgroupRoot, "cpu", "cpu.stat");
            var cpuUsagePath = Path.Combine(CgroupRoot, "cpuacct", "cpuacct.usage");
            var memoryLimitPath = Path.Combine(CgroupRoot, "memory", "memory.limit_in_bytes");
            var memoryUsagePath = Path.Combine(CgroupRoot, "memory", "memory.usage_in_bytes");

            if (!File.Exists(quotaPath) && !File.Exists(memoryLimitPath))
            {
                return null;
            }

            double? cpuLimitCores = null;
            if (File.Exists(quotaPath) && File.Exists(periodPath) &&
                long.TryParse(File.ReadAllText(quotaPath).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var quota) && quota > 0 &&
                long.TryParse(File.ReadAllText(periodPath).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var period) && period > 0)
            {
                cpuLimitCores = (double)quota / period;
            }

            long? cpuUsageMicroseconds = null;
            if (File.Exists(cpuUsagePath) &&
                long.TryParse(File.ReadAllText(cpuUsagePath).Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var usageNanoseconds))
            {
                cpuUsageMicroseconds = usageNanoseconds / 1000;
            }

            long? cpuThrottledPeriods = null;
            long? cpuThrottledMicroseconds = null;
            if (File.Exists(cpuStatPath))
            {
                foreach (var line in File.ReadAllLines(cpuStatPath))
                {
                    var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length != 2 || !long.TryParse(fields[1], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out var value))
                    {
                        continue;
                    }

                    switch (fields[0])
                    {
                        case "nr_throttled":
                            cpuThrottledPeriods = value;
                            break;
                        case "throttled_time":
                            cpuThrottledMicroseconds = value / 1000;
                            break;
                    }
                }
            }

            long? memoryLimitBytes = null;
            if (File.Exists(memoryLimitPath) &&
                long.TryParse(File.ReadAllText(memoryLimitPath).Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var limit) && limit < long.MaxValue / 2)
            {
                memoryLimitBytes = limit;
            }

            long? memoryUsageBytes = null;
            if (File.Exists(memoryUsagePath) &&
                long.TryParse(File.ReadAllText(memoryUsagePath).Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var usage))
            {
                memoryUsageBytes = usage;
            }

            return new ContainerResourceLimits(cpuLimitCores, cpuUsageMicroseconds, cpuThrottledPeriods,
                cpuThrottledMicroseconds, memoryLimitBytes, memoryUsageBytes);
        }
    }
}
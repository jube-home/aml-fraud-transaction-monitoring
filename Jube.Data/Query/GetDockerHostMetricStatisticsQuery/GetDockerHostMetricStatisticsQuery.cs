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

namespace Jube.Data.Query.GetDockerHostMetricStatisticsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Helpers;
    using LinqToDB;
    using Poco;
    using Dto.Payload;

    public class GetDockerHostMetricStatisticsQuery(DbContext dbContext)
    {
        public async Task<PayloadStatistics> ExecuteAsync(DateTime? from, DateTime? to, string search,
            double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            var rows = await BuildFilteredQuery(from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["containersTotal"] =
                [
                    .. rows.Where(w => w.ContainersTotal.HasValue)
                        .Select(s => (double)s.ContainersTotal!.Value)
                ],
                ["containersRunning"] =
                [
                    .. rows.Where(w => w.ContainersRunning.HasValue)
                        .Select(s => (double)s.ContainersRunning!.Value)
                ],
                ["containersPaused"] =
                [
                    .. rows.Where(w => w.ContainersPaused.HasValue)
                        .Select(s => (double)s.ContainersPaused!.Value)
                ],
                ["containersStopped"] =
                [
                    .. rows.Where(w => w.ContainersStopped.HasValue)
                        .Select(s => (double)s.ContainersStopped!.Value)
                ],
                ["imagesCount"] =
                [
                    .. rows.Where(w => w.ImagesCount.HasValue)
                        .Select(s => (double)s.ImagesCount!.Value)
                ],
                ["nCpu"] = [.. rows.Where(w => w.NCpu.HasValue).Select(s => (double)s.NCpu!.Value)],
                ["memTotalBytes"] =
                [
                    .. rows.Where(w => w.MemTotalBytes.HasValue)
                        .Select(s => (double)s.MemTotalBytes!.Value)
                ],
                ["layersSizeBytes"] =
                [
                    .. rows.Where(w => w.LayersSizeBytes.HasValue)
                        .Select(s => (double)s.LayersSizeBytes!.Value)
                ],
                ["imagesSizeBytes"] =
                [
                    .. rows.Where(w => w.ImagesSizeBytes.HasValue)
                        .Select(s => (double)s.ImagesSizeBytes!.Value)
                ],
                ["reclaimableImagesBytes"] =
                [
                    .. rows.Where(w => w.ReclaimableImagesBytes.HasValue)
                        .Select(s => (double)s.ReclaimableImagesBytes!.Value)
                ],
                ["containersDiskBytes"] =
                [
                    .. rows.Where(w => w.ContainersDiskBytes.HasValue)
                        .Select(s => (double)s.ContainersDiskBytes!.Value)
                ],
                ["volumesSizeBytes"] =
                [
                    .. rows.Where(w => w.VolumesSizeBytes.HasValue)
                        .Select(s => (double)s.VolumesSizeBytes!.Value)
                ],
                ["buildCacheSizeBytes"] =
                [
                    .. rows.Where(w => w.BuildCacheSizeBytes.HasValue)
                        .Select(s => (double)s.BuildCacheSizeBytes!.Value)
                ]
            });
        }

        private IQueryable<DockerHostMetric> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.DockerHostMetric.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.CreatedDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.CreatedDate <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();

                query = query.Where(w =>
                    (w.Instance != null && w.Instance.ToLower().Contains(lowerSearch)) ||
                    (w.OperatingSystem != null && w.OperatingSystem.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }
    }
}
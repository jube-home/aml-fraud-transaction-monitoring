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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Helpers;
using Jube.Data.Poco;
using Jube.Dto.Payload;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class DockerHostMetricRepository(DbContext dbContext)
    {
        public async Task<DockerHostMetric> InsertAsync(DockerHostMetric model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<DockerHostMetric>> GetLastAsync(int take, DateTime? from, DateTime? to,
            string search, double? samplePercentage, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, search, samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(DateTime? from, DateTime? to, string search, double? samplePercentage,
            CancellationToken token = default)
        {
            return BuildFilteredQuery(from, to, search, samplePercentage).CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(DateTime? from, DateTime? to, string search,
            double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            var rows = await BuildFilteredQuery(from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["containersTotal"] = rows.Where(w => w.ContainersTotal.HasValue)
                    .Select(s => (double)s.ContainersTotal!.Value).ToArray(),
                ["containersRunning"] = rows.Where(w => w.ContainersRunning.HasValue)
                    .Select(s => (double)s.ContainersRunning!.Value).ToArray(),
                ["containersPaused"] = rows.Where(w => w.ContainersPaused.HasValue)
                    .Select(s => (double)s.ContainersPaused!.Value).ToArray(),
                ["containersStopped"] = rows.Where(w => w.ContainersStopped.HasValue)
                    .Select(s => (double)s.ContainersStopped!.Value).ToArray(),
                ["imagesCount"] = rows.Where(w => w.ImagesCount.HasValue)
                    .Select(s => (double)s.ImagesCount!.Value).ToArray(),
                ["nCpu"] = rows.Where(w => w.NCpu.HasValue).Select(s => (double)s.NCpu!.Value).ToArray(),
                ["memTotalBytes"] = rows.Where(w => w.MemTotalBytes.HasValue)
                    .Select(s => (double)s.MemTotalBytes!.Value).ToArray(),
                ["layersSizeBytes"] = rows.Where(w => w.LayersSizeBytes.HasValue)
                    .Select(s => (double)s.LayersSizeBytes!.Value).ToArray(),
                ["imagesSizeBytes"] = rows.Where(w => w.ImagesSizeBytes.HasValue)
                    .Select(s => (double)s.ImagesSizeBytes!.Value).ToArray(),
                ["reclaimableImagesBytes"] = rows.Where(w => w.ReclaimableImagesBytes.HasValue)
                    .Select(s => (double)s.ReclaimableImagesBytes!.Value).ToArray(),
                ["containersDiskBytes"] = rows.Where(w => w.ContainersDiskBytes.HasValue)
                    .Select(s => (double)s.ContainersDiskBytes!.Value).ToArray(),
                ["volumesSizeBytes"] = rows.Where(w => w.VolumesSizeBytes.HasValue)
                    .Select(s => (double)s.VolumesSizeBytes!.Value).ToArray(),
                ["buildCacheSizeBytes"] = rows.Where(w => w.BuildCacheSizeBytes.HasValue)
                    .Select(s => (double)s.BuildCacheSizeBytes!.Value).ToArray()
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

        private static IOrderedQueryable<DockerHostMetric> ApplySort(IQueryable<DockerHostMetric> query,
            string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "containersTotal" => query.OrderByField(o => o.ContainersTotal, descending),
                "containersRunning" => query.OrderByField(o => o.ContainersRunning, descending),
                "containersPaused" => query.OrderByField(o => o.ContainersPaused, descending),
                "containersStopped" => query.OrderByField(o => o.ContainersStopped, descending),
                "imagesCount" => query.OrderByField(o => o.ImagesCount, descending),
                "nCpu" => query.OrderByField(o => o.NCpu, descending),
                "memTotalBytes" => query.OrderByField(o => o.MemTotalBytes, descending),
                "dockerVersion" => query.OrderByField(o => o.DockerVersion, descending),
                "apiVersion" => query.OrderByField(o => o.ApiVersion, descending),
                "kernelVersion" => query.OrderByField(o => o.KernelVersion, descending),
                "operatingSystem" => query.OrderByField(o => o.OperatingSystem, descending),
                "osType" => query.OrderByField(o => o.OsType, descending),
                "architecture" => query.OrderByField(o => o.Architecture, descending),
                "layersSizeBytes" => query.OrderByField(o => o.LayersSizeBytes, descending),
                "imagesSizeBytes" => query.OrderByField(o => o.ImagesSizeBytes, descending),
                "reclaimableImagesBytes" => query.OrderByField(o => o.ReclaimableImagesBytes, descending),
                "containersDiskBytes" => query.OrderByField(o => o.ContainersDiskBytes, descending),
                "volumesSizeBytes" => query.OrderByField(o => o.VolumesSizeBytes, descending),
                "buildCacheSizeBytes" => query.OrderByField(o => o.BuildCacheSizeBytes, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}
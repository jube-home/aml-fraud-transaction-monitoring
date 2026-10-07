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

namespace Jube.Service.Query.VersionHistory
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Query;
    using log4net;
    using Microsoft.Extensions.Localization;

    public interface IVersionHistoryService<TSelf, TVersion>
        where TSelf : IVersionHistoryService<TSelf, TVersion>
        where TVersion : class
    {
        static abstract Task<TSelf> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, CancellationToken token = default);

        Task<TVersion?> GetByIdAsync(int id, CancellationToken token = default);

        Task<List<TVersion>> GetByParentIdAsync(int parentId, CancellationToken token = default);

        Task<List<TVersion>> GetByDateRangeAsync(int parentId, DateTime from, DateTime to,
            CancellationToken token = default);

        Task<TVersion?> GetByIdAndDateRangeAsync(int id, DateTime from, DateTime to,
            CancellationToken token = default);

        Task<TVersion?> GetLatestAsync(int parentId, CancellationToken token = default);

        Task<IReadOnlyList<VersionFieldChange>> CompareAsync(int fromId, int toId,
            CancellationToken token = default);
    }
}
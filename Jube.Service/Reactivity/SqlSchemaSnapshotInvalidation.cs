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

using Jube.Data.Reporting.Check;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Reactivity.Models;

namespace Jube.Service.Reactivity
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class SqlSchemaSnapshotInvalidation
    {
        public const string SchemaChangeArea = "SchemaChange";

        public static IDisposable Attach(IServiceChangeBus serviceChangeBus)
        {
            return serviceChangeBus.Subscribe(change =>
            {
                if (string.Equals(change.Area, SchemaChangeArea, StringComparison.Ordinal))
                {
                    SqlSchemaSnapshotCache.Invalidate();
                }

                return Task.CompletedTask;
            });
        }

        public static Task AnnounceAsync(IServiceChangeBus serviceChangeBus, string actor,
            CancellationToken token = default)
        {
            SqlSchemaSnapshotCache.Invalidate();

            return serviceChangeBus.PublishAsync(new ServiceChangeEvent(SchemaChangeArea, 0, ServiceChangeKind.Updated,
                null, null, actor, DateTimeOffset.UtcNow, Guid.NewGuid().ToString("N")), token);
        }
    }
}

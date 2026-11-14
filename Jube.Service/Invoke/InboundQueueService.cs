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

using System.Text.Json;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetEntityAnalysisModelApiUsersQuery;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelInvoke.Exceptions;
using log4net;

namespace Jube.Service.Invoke
{
    public sealed class InboundQueueService(
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        string? userName,
        Func<DbContext> dbContextFactory)
    {
        private static readonly Lock depthLock = new();
        private static DateTime depthCheckedAt = DateTime.MinValue;
        private static bool depthFull;
        private static int depthMaximum = -1;

        public async Task<InvokeResult> EnqueueAsync(string? guidRouteValue, Func<int, Task<MemoryStream>> readBodyAsync,
            CancellationToken token = default)
        {
            if (!"True".Equals(dynamicEnvironment.AppSettings("EnablePublicInvokeController"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return InvokeResult.Empty(404);
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                return InvokeResult.Forbidden();
            }

            if (!Guid.TryParse(guidRouteValue, out var guid))
            {
                return InvokeResult.Empty(404);
            }

            MemoryStream body;

            try
            {
                body = await readBodyAsync(int.Parse(dynamicEnvironment.AppSettings("MaxInvokeControllerRequestBytes")))
                    .ConfigureAwait(false);
            }
            catch (ExceededBytesException)
            {
                return InvokeResult.Json(400, "Exceeded the maximum allowed bytes in POST body.");
            }
            catch (ZeroBytesException)
            {
                return InvokeResult.Json(400, "Empty POST body.");
            }

            var bytes = body.ToArray();

            if (!IsJson(bytes))
            {
                return InvokeResult.Json(400, "The body is not valid JSON.");
            }

            try
            {
                await using var dbContext = dbContextFactory();

                var model = await new EntityAnalysisModelRepository(dbContext, userName).GetByGuidAsync(guid, token)
                    .ConfigureAwait(false);

                if (model is null || model.Active != 1)
                {
                    return InvokeResult.Empty(404);
                }

                var allowed = (await new GetEntityAnalysisModelApiUsersQuery(dbContext)
                        .ExecuteAsync(model.Id, token).ConfigureAwait(false))
                    .Any(u => string.Equals(u.Username, userName, StringComparison.Ordinal));

                if (!allowed)
                {
                    return InvokeResult.Forbidden();
                }

                var repository = new TransactionInboundQueueRepository(dbContext);

                if (await IsFullAsync(repository, token).ConfigureAwait(false))
                {
                    return InvokeResult.Json(429, "The queue is full; retry later.");
                }

                var row = new TransactionInboundQueue
                {
                    EntityAnalysisModelGuid = guid, Payload = bytes, EnqueuedBy = userName
                };

                await repository.InsertAsync(row, token).ConfigureAwait(false);

                return InvokeResult.Json(202, new { id = row.Id });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error("InboundQueue: could not enqueue a transaction.", ex);

                return InvokeResult.Empty(500);
            }
        }

        private async Task<bool> IsFullAsync(TransactionInboundQueueRepository repository, CancellationToken token)
        {
            var maximum = int.Parse(dynamicEnvironment.AppSettings("InboundQueueMaximumDepth"));

            lock (depthLock)
            {
                if (depthMaximum == maximum && (DateTime.UtcNow - depthCheckedAt).TotalSeconds < 2)
                {
                    return depthFull;
                }
            }

            var pending = await repository.PendingBoundedAsync(maximum + 1, token).ConfigureAwait(false);

            lock (depthLock)
            {
                depthCheckedAt = DateTime.UtcNow;
                depthMaximum = maximum;
                depthFull = pending > maximum;

                return depthFull;
            }
        }

        public static bool IsJson(byte[] bytes)
        {
            try
            {
                var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { MaxDepth = 64 });

                while (reader.Read())
                {
                }

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}

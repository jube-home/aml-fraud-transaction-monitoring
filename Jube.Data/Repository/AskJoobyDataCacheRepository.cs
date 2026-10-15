/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using Jube.Data.Repository.Models.AskJoobyDataCache;
using LinqToDB.Data;

namespace Jube.Data.Repository
{
    public class AskJoobyDataCacheRepository(DbContext dbContext, int? tenantRegistryId, string userName)
    {
        private const string InsertSql = """
                                         INSERT INTO "AskJoobyDataCache"
                                             ("PointerKey", "TenantRegistryId", "AskJoobyConversationId", "CreatedUser",
                                              "Label", "ColumnsJson", "RowCount", "DataJson", "CreatedDate", "ExpiresAt")
                                         VALUES (@pointerKey, @tenantRegistryId, @conversationId, @createdUser,
                                              @label, @columnsJson::jsonb, @rowCount, @dataJson::jsonb, @createdDate, @expiresAt)
                                         RETURNING "Id";
                                         """;

        private const string GetByPointerKeySql = """
                                                  SELECT "ColumnsJson", "DataJson"
                                                  FROM "AskJoobyDataCache"
                                                  WHERE "PointerKey" = @pointerKey
                                                    AND "TenantRegistryId" = @tenantRegistryId
                                                    AND ("ExpiresAt" IS NULL OR "ExpiresAt" > now())
                                                  """;

        private const string ListByConversationSql = """
                                                     SELECT "Id", "PointerKey", "Label", "RowCount", "ColumnsJson", "DataJson", "CreatedDate"
                                                     FROM "AskJoobyDataCache"
                                                     WHERE "AskJoobyConversationId" = @conversationId
                                                       AND "TenantRegistryId" = @tenantRegistryId
                                                     ORDER BY "Id"
                                                     """;

        public async Task<int> InsertAsync(string pointerKey, string columnsJson, string dataJson, int rowCount,
            string label, int? conversationId, DateTime expiresAt, CancellationToken token = default)
        {
            var id = (await dbContext.QueryToListAsync<int>(InsertSql, token,
                    new DataParameter("pointerKey", pointerKey),
                    new DataParameter("tenantRegistryId",
                        tenantRegistryId.HasValue ? tenantRegistryId.Value : DBNull.Value),
                    new DataParameter("conversationId",
                        conversationId.HasValue ? conversationId.Value : DBNull.Value),
                    new DataParameter("createdUser", (object)userName ?? DBNull.Value),
                    new DataParameter("label", (object)label ?? DBNull.Value),
                    new DataParameter("columnsJson", columnsJson),
                    new DataParameter("rowCount", rowCount),
                    new DataParameter("dataJson", dataJson),
                    new DataParameter("createdDate", DateTime.UtcNow),
                    new DataParameter("expiresAt", expiresAt))
                .ConfigureAwait(false)).Single();

            return id;
        }

        public async Task<AskJoobyDataCacheJsonDto> GetByPointerKeyAsync(string pointerKey,
            CancellationToken token = default)
        {
            var rows = await dbContext.QueryToListAsync<AskJoobyDataCacheJsonDto>(GetByPointerKeySql, token,
                    new DataParameter("pointerKey", pointerKey),
                    new DataParameter("tenantRegistryId",
                        tenantRegistryId.HasValue ? tenantRegistryId.Value : DBNull.Value))
                .ConfigureAwait(false);

            return rows.SingleOrDefault();
        }

        public Task<List<AskJoobyDataCacheListItemJsonDto>> ListByConversationAsync(int conversationId,
            CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<AskJoobyDataCacheListItemJsonDto>(ListByConversationSql, token,
                new DataParameter("conversationId", conversationId),
                new DataParameter("tenantRegistryId",
                    tenantRegistryId.HasValue ? tenantRegistryId.Value : DBNull.Value));
        }
    }
}
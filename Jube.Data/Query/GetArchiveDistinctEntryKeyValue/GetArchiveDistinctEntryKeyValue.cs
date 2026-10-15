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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.ResilientNpgsqlConnection;
using Jube.ResilientNpgsqlConnection.Extensions.Jube.ResilientNpgsqlConnection;
using log4net;

namespace Jube.Data.Query.GetArchiveDistinctEntryKeyValue
{
    public class GetArchiveDistinctEntryKeyValue(DbContext dbContext, ILog log)
    {
        public async Task<List<string>> ExecuteAsync(Guid entityAnalysisModelGuid,
            string key, DateTime dateFrom, DateTime dateTo, CancellationToken token = default)
        {
            var value = new List<string>();

            try
            {
                const string sql = "select distinct \"Json\" -> 'payload' ->> (@key)" +
                                   " from \"Archive\" a inner join \"EntityAnalysisModel\" e on a.\"EntityAnalysisModelId\" = e.\"Id\"" +
                                   " where e.\"Guid\" = (@entityAnalysisModelGuid)" +
                                   " and \"Json\" -> 'payload' ->> (@key) = (@value)" +
                                   " and \"CreatedDate\" > (@dateFrom) and \"CreatedDate\" < (@dateTo)";

                await using var command =
                    new ResilientNpgsqlCommand(
                        (ResilientNpgsqlConnection.ResilientNpgsqlConnection)dbContext.Connection, sql);
                command.Parameters.AddWithValue("key", key);
                command.Parameters.AddWithValue("dateFrom", dateFrom);
                command.Parameters.AddWithValue("dateTo", dateTo);
                command.Parameters.AddWithValue("entityAnalysisModelGuid", entityAnalysisModelGuid);
                await command.PrepareAsync(token).ConfigureAwait(false);

                await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    if (!await reader.IsDBNullAsync(0, token))
                    {
                        value.Add(reader.GetValue(0).ToString());
                    }
                }

                await reader.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Archive SQL: Has created an exception as {ex}.");
            }

            return value;
        }

        public async Task<List<string>> ExecuteAsync(Guid entityAnalysisModelGuid,
            string key, CancellationToken token = default)
        {
            var value = new List<string>();

            try
            {
                const string sql = "select distinct \"Json\" -> 'payload' ->> (@key)" +
                                   " from \"Archive\" a inner join \"EntityAnalysisModel\" e on a.\"EntityAnalysisModelId\" = e.\"Id\"" +
                                   " where e.\"Guid\" = (@entityAnalysisModelGuid)";

                await using var command =
                    new ResilientNpgsqlCommand(
                        (ResilientNpgsqlConnection.ResilientNpgsqlConnection)dbContext.Connection, sql);
                command.Parameters.AddWithValue("entityAnalysisModelGuid", entityAnalysisModelGuid);
                command.Parameters.AddWithValue("key", key);
                await command.PrepareAsync(token).ConfigureAwait(false);

                await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    if (!await reader.IsDBNullAsync(0, token))
                    {
                        value.Add(reader.GetValue(0).ToString());
                    }
                }

                await reader.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Archive SQL: Has created an exception as {ex}.");
            }

            return value;
        }
    }
}
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

using System.Diagnostics;
using Jube.Data.Reporting;
using Jube.Service.Charts.Datasource;
using Jube.Service.CaseDispatch.Models;
using Jube.Service.DynamicSql;
using log4net;

namespace Jube.Service.CaseDispatch
{
    public sealed class CaseSqlRunner(string connectionString, Func<IReadOnlyDictionary<string, string>> tables, ILog log)
    {
        public CaseSqlRunner(string connectionString, string userName, ILog log) : this(connectionString,
            () => DynamicSqlTableAccess.BuildVisualisationAllowedTables(userName, _ => true), log)
        {
        }

        public static CaseSqlRunner ForTenant(string connectionString, int tenantRegistryId, ILog log)
        {
            return new CaseSqlRunner(connectionString,
                () => DynamicSqlTableAccess.BuildTenantAllowedTables(tenantRegistryId), log);
        }

        public const int MaximumRows = 200;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);

        public async Task<CaseRows> RunAsync(string? sql, int caseId, string? caseKeyValue,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return CaseRows.None;
            }

            var allowed = tables();
            var clock = Stopwatch.StartNew();

            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);

            limit.CancelAfter(Timeout);

            try
            {
                using var postgres = new Postgres(connectionString, log);

                var result = await postgres.ExecuteLimitedByNamedParametersAsync(sql,
                    new Dictionary<string, object>
                    {
                        ["Id"] = caseId,
                        ["CaseKeyValue"] = (object?)caseKeyValue ?? DBNull.Value
                    }, allowed, MaximumRows, limit.Token).ConfigureAwait(false);

                var names = result.Columns.Select(c => c.Name).ToList();

                var rows = result.Rows.Select(row =>
                {
                    var map = new Dictionary<string, object?>(names.Count, StringComparer.OrdinalIgnoreCase);

                    for (var i = 0; i < names.Count; i++)
                    {
                        map[names[i]] = DatasourceCardValue.Normalise(row[i]);
                    }

                    return (IReadOnlyDictionary<string, object?>)map;
                }).ToList();

                return new CaseRows([.. result.Columns.Select(c => (c.Name, c.DataTypeName))], rows,
                    result.Truncated, (int)clock.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new CaseSqlException("The SQL took too long and was stopped.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new CaseSqlException(e is InvalidOperationException or ArgumentException
                    ? e.Message
                    : "The SQL could not be run.", e);
            }
        }
    }
}

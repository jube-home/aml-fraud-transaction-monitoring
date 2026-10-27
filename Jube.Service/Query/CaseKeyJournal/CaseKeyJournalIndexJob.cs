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


using System.Security.Cryptography;
using System.Text;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using LinqToDB;
using log4net;
using Npgsql;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed class CaseKeyJournalIndexJob(string connectionString, string? migrationConnectionString, ILog log,
        int minimumRows, int maximumIndexes)
    {
        public const string IndexPrefix = "IX_Archive_Journal_";
        public static readonly TimeSpan RetryAfter = TimeSpan.FromHours(1);
        public static readonly TimeSpan AbandonedAfter = TimeSpan.FromDays(1);
        public const long AdvisoryLock = 7304011;
        private const int MaximumErrorLength = 500;

        public static string IndexName(string key)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

            return IndexPrefix + hash[..16].ToLowerInvariant();
        }

        public static string CreateStatement(string key)
        {
            return $"create index concurrently if not exists \"{IndexName(key)}\" on \"Archive\" " +
                   $"(\"EntityAnalysisModelId\", {GetCaseKeyJournalRowsQuery.IndexExpression(key)}, \"Id\" desc)";
        }

        public async Task<CaseKeyJournalIndexResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                connectionString, log);

            var repository = new CaseKeyJournalKeyIndexRepository(dbContext);

            var archived = await dbContext.Archive.Take(minimumRows).CountAsync(token).ConfigureAwait(false);

            if (archived < minimumRows)
            {
                return new CaseKeyJournalIndexResult([], [], [], "BelowMinimumRows");
            }

            await using var lockConnection = new NpgsqlConnection(DdlConnectionString());
            await lockConnection.OpenAsync(token).ConfigureAwait(false);

            await using (var attempt = new NpgsqlCommand("select pg_try_advisory_lock(@key)", lockConnection))
            {
                attempt.Parameters.AddWithValue("key", AdvisoryLock);

                if (await attempt.ExecuteScalarAsync(token).ConfigureAwait(false) is not true)
                {
                    return new CaseKeyJournalIndexResult([], [], [], "AnotherNodeIsIndexing");
                }
            }

            var lost = await ReconcileAsync(repository, token).ConfigureAwait(false);
            var candidates = await CandidatesAsync(dbContext, token).ConfigureAwait(false);
            var existing = (await repository.GetAsync(token).ConfigureAwait(false))
                .ToDictionary(row => row.KeyName, StringComparer.Ordinal);

            var held = existing.Values.Count(row => row.StateId == (byte)CaseKeyJournalKeyIndexState.Ready);
            var created = new List<string>();
            var failed = new List<string>();

            foreach (var key in candidates)
            {
                token.ThrowIfCancellationRequested();

                if (existing.TryGetValue(key, out var row) && !Due(row))
                {
                    continue;
                }

                if (held >= maximumIndexes)
                {
                    return new CaseKeyJournalIndexResult(created, failed, lost, "MaximumIndexes");
                }

                if (await BuildAsync(repository, key, token).ConfigureAwait(false))
                {
                    created.Add(key);
                    held++;
                }
                else
                {
                    failed.Add(key);
                }
            }

            return new CaseKeyJournalIndexResult(created, failed, lost, null);
        }

        private static bool Due(CaseKeyJournalKeyIndex row)
        {
            var now = DateTime.UtcNow;

            return row.StateId switch
            {
                (byte)CaseKeyJournalKeyIndexState.Ready => false,
                (byte)CaseKeyJournalKeyIndexState.Failed => now - row.UpdatedDate >= RetryAfter,
                _ => now - row.UpdatedDate >= AbandonedAfter
            };
        }

        private static async Task<List<string>> CandidatesAsync(DbContext dbContext, CancellationToken token)
        {
            var caseKeys = await dbContext.Case.Where(c => c.CaseKey != null).Select(c => c.CaseKey).Distinct()
                .ToListAsync(token).ConfigureAwait(false);

            var drillPaths = await dbContext.CaseWorkflowXPath
                .Where(x => x.Drill == 1 && (x.Deleted == null || x.Deleted == 0)).Select(x => x.XPath).Distinct()
                .ToListAsync(token).ConfigureAwait(false);

            return caseKeys.Concat(drillPaths.Select(JournalTransactionReader.PayloadKey))
                .Where(key => !string.IsNullOrEmpty(key) && GetCaseKeyJournalRowsQuery.IsIndexableKey(key))
                .Select(key => key!).Distinct(StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal)
                .ToList();
        }

        private async Task<List<string>> ReconcileAsync(CaseKeyJournalKeyIndexRepository repository,
            CancellationToken token)
        {
            var ready = (await repository.GetAsync(token).ConfigureAwait(false))
                .Where(row => row.StateId == (byte)CaseKeyJournalKeyIndexState.Ready).ToList();

            var lost = new List<string>();

            if (ready.Count == 0)
            {
                return lost;
            }

            await using var connection = new NpgsqlConnection(DdlConnectionString());
            await connection.OpenAsync(token).ConfigureAwait(false);

            foreach (var row in ready)
            {
                if (await IsValidAsync(connection, row.IndexName, token).ConfigureAwait(false))
                {
                    continue;
                }

                await repository.MarkAsync(row.KeyName, row.IndexName, CaseKeyJournalKeyIndexState.Failed,
                    "The index is missing or invalid.", false, token).ConfigureAwait(false);
                lost.Add(row.KeyName);
            }

            return lost;
        }

        private async Task<bool> BuildAsync(CaseKeyJournalKeyIndexRepository repository, string key,
            CancellationToken token)
        {
            var name = IndexName(key);

            await repository.MarkAsync(key, name, CaseKeyJournalKeyIndexState.Building, null, true, token)
                .ConfigureAwait(false);

            try
            {
                await using var connection = new NpgsqlConnection(DdlConnectionString());
                await connection.OpenAsync(token).ConfigureAwait(false);

                await ExecuteAsync(connection, $"drop index concurrently if exists \"{name}\"", token)
                    .ConfigureAwait(false);
                await ExecuteAsync(connection, CreateStatement(key), token).ConfigureAwait(false);

                if (await IsValidAsync(connection, name, token).ConfigureAwait(false))
                {
                    await repository.MarkAsync(key, name, CaseKeyJournalKeyIndexState.Ready, null, false, token)
                        .ConfigureAwait(false);

                    return true;
                }

                await ExecuteAsync(connection, $"drop index concurrently if exists \"{name}\"", token)
                    .ConfigureAwait(false);
                await repository.MarkAsync(key, name, CaseKeyJournalKeyIndexState.Failed,
                    "The index was built but is not valid.", false, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"CaseKeyJournalIndexJob: building the index for {key} failed as {ex}.");

                var message = ex.Message.Length > MaximumErrorLength ? ex.Message[..MaximumErrorLength] : ex.Message;

                await repository.MarkAsync(key, name, CaseKeyJournalKeyIndexState.Failed, message, false, token)
                    .ConfigureAwait(false);
            }

            return false;
        }

        private string DdlConnectionString()
        {
            return string.IsNullOrWhiteSpace(migrationConnectionString) ? connectionString : migrationConnectionString;
        }

        private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken token)
        {
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 0 };

            await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        private static async Task<bool> IsValidAsync(NpgsqlConnection connection, string name,
            CancellationToken token)
        {
            await using var command = new NpgsqlCommand(
                "select i.indisvalid from pg_index i join pg_class c on c.oid = i.indexrelid " +
                "where c.relname = @name and c.relnamespace = 'public'::regnamespace", connection);
            command.Parameters.AddWithValue("name", name);

            return await command.ExecuteScalarAsync(token).ConfigureAwait(false) is true;
        }
    }
}

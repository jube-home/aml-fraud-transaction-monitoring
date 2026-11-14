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
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Engine.EntityAnalysisModelManager.Helpers;
using Jube.Engine.Sanctions.Models;
using log4net;
using Microsoft.VisualBasic.FileIO;

namespace Jube.Engine.Sanctions
{
    using SanctionEntry = Data.Poco.SanctionEntry;

    public static class SanctionEntryFileImporter
    {
        public const int MaximumRows = 5_000_000;
        public const double MaximumRemovalRatio = 0.5;
        public const int RemovalGuardMinimumActive = 100;

        public static Task<SanctionEntryFileImportResult> ImportAsync(TextFieldParser tfp,
            int sanctionEntrySourceId, string multiPartStringIndex, int? referenceIndex, int skip,
            Func<SanctionEntryFileRecord, CancellationToken, Task> onRecordAsync,
            Func<SanctionEntryFileRejection, CancellationToken, Task> onRejectedAsync, ILog log,
            CancellationToken token = default)
        {
            return ImportAsync(tfp, sanctionEntrySourceId, multiPartStringIndex, referenceIndex, skip,
                SanctionEntryFileOptions.None, onRecordAsync, onRejectedAsync, log, token);
        }

        public static async Task<SanctionEntryFileImportResult> ImportAsync(TextFieldParser tfp,
            int sanctionEntrySourceId, string multiPartStringIndex, int? referenceIndex, int skip,
            SanctionEntryFileOptions options,
            Func<SanctionEntryFileRecord, CancellationToken, Task> onRecordAsync,
            Func<SanctionEntryFileRejection, CancellationToken, Task> onRejectedAsync, ILog log,
            CancellationToken token = default)
        {
            options ??= SanctionEntryFileOptions.None;

            var seenHashes = new HashSet<string>();
            var totalRows = 0;
            var rejectedRows = 0;
            var completed = true;

            try
            {
                tfp.TextFieldType = FieldType.Delimited;
                var i = 1;

                while (!tfp.EndOfData)
                {
                    if (i > MaximumRows)
                    {
                        completed = false;
                        log.Error($"Sanctions Loader: the file exceeds {MaximumRows} rows and was abandoned.");

                        break;
                    }

                    var isDataRow = i > skip;
                    var rejected = false;

                    try
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Sanctions Loader: Has loaded the database connection.  Is processing record {i}.  Will now build the SQL Command object.");
                        }

                        var data = tfp.ReadFields();

                        if (isDataRow)
                        {
                            if (data is { Length: > 1 } && !PassesFilter(data, options))
                            {
                                if (log.IsDebugEnabled)
                                {
                                    log.Debug($"Sanctions Loader: record {i} is filtered out by the source filter.");
                                }
                            }
                            else if (data is { Length: > 1 })
                            {
                                var record = BuildRecord(data, sanctionEntrySourceId, multiPartStringIndex,
                                    referenceIndex);

                                if (record != null)
                                {
                                    seenHashes.Add(record.Hash);
                                    await onRecordAsync(record, token).ConfigureAwait(false);

                                    foreach (var alias in BuildAliasRecords(data, sanctionEntrySourceId, record,
                                                 options))
                                    {
                                        seenHashes.Add(alias.Hash);
                                        await onRecordAsync(alias, token).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    rejected = true;

                                    await RejectAsync(onRejectedAsync, i, data,
                                            SanctionEntryRejectionReason.NoReferenceIndexConfigured, token)
                                        .ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                rejected = true;

                                await RejectAsync(onRejectedAsync, i, data,
                                    SanctionEntryRejectionReason.InsufficientFields, token).ConfigureAwait(false);

                                if (log.IsInfoEnabled)
                                {
                                    log.Info($"Sanctions Loader: record {i} has no data.");
                                }
                            }
                        }
                        else if (log.IsInfoEnabled)
                        {
                            log.Info($"Sanctions Loader: Skipped header row {i}");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (isDataRow && !rejected)
                        {
                            rejected = true;

                            await RejectAsync(onRejectedAsync, i, null, SanctionEntryRejectionReason.ParseError,
                                token).ConfigureAwait(false);
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Sanctions Loader: Error loading record {ex}");
                        }
                    }
                    finally
                    {
                        if (isDataRow)
                        {
                            totalRows += 1;

                            if (rejected)
                            {
                                rejectedRows += 1;
                            }
                        }

                        i += 1;

                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Sanctions Loader: Moving to record {i}.");
                        }
                    }
                }

                tfp.Close();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                completed = false;
                log.Error($"ProcessTextFieldParser: has produced an error {ex}");
            }

            return new SanctionEntryFileImportResult(seenHashes, totalRows, rejectedRows, completed);
        }

        public static async Task<IReadOnlyCollection<SanctionEntry>> ReconcileRemovedAsync(
            SanctionEntryRepository repository, int sanctionEntrySourceId,
            IReadOnlyCollection<string> currentHashes, string userName, ILog log,
            CancellationToken token = default)
        {
            var removed = new List<SanctionEntry>();

            if (currentHashes.Count == 0)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"Sanctions Loader: Skipped removal reconciliation for source {sanctionEntrySourceId} as no records were parsed from the file this run. This usually means the source file failed to download or parse -- investigate before assuming the list is simply empty.");
                }

                return removed;
            }

            try
            {
                var active = (await repository
                    .GetActiveBySanctionEntrySourceIdAsync(sanctionEntrySourceId, token)
                    .ConfigureAwait(false)).ToList();

                var missing = active.Where(entry => !currentHashes.Contains(entry.SanctionEntryHash)).ToList();

                if (active.Count >= RemovalGuardMinimumActive && missing.Count > active.Count * MaximumRemovalRatio)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"Sanctions Loader: Skipped removal reconciliation for source {sanctionEntrySourceId} as it would remove {missing.Count} of {active.Count} active entries, over the {MaximumRemovalRatio:P0} safety limit. A truncated or wrong file is the likely cause; investigate before delisting.");
                    }

                    return removed;
                }

                foreach (var entry in missing)
                {
                    token.ThrowIfCancellationRequested();

                    await repository.DeleteAsync(entry.Id, userName, token).ConfigureAwait(false);
                    removed.Add(entry);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Sanctions Loader: Has removed record {entry.Id} for source {sanctionEntrySourceId} as it is no longer present in the source file.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"ReconcileRemovedAsync: has produced an error {ex}");
            }

            return removed;
        }

        public static string NormalizeElementValue(string raw)
        {
            return SanctionTextFolding.Spaced(raw);
        }

        private static Task RejectAsync(Func<SanctionEntryFileRejection, CancellationToken, Task> onRejectedAsync,
            int rowNumber, string[] data, SanctionEntryRejectionReason reasonId, CancellationToken token)
        {
            if (onRejectedAsync == null)
            {
                return Task.CompletedTask;
            }

            var rawData = data != null ? string.Join(',', data) : null;

            return onRejectedAsync(new SanctionEntryFileRejection(rowNumber, rawData, reasonId), token);
        }

        private static bool PassesFilter(string[] data, SanctionEntryFileOptions options)
        {
            if (options.FilterIndex is null || string.IsNullOrWhiteSpace(options.FilterValues))
            {
                return true;
            }

            if (options.FilterIndex.Value < 0 || options.FilterIndex.Value >= data.Length)
            {
                return false;
            }

            var value = data[options.FilterIndex.Value].Trim();

            return options.FilterValues
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<SanctionEntryFileRecord> BuildAliasRecords(string[] data, int sanctionEntrySourceId,
            SanctionEntryFileRecord primary, SanctionEntryFileOptions options)
        {
            if (options.AliasIndex is null || options.AliasIndex.Value < 0 || options.AliasIndex.Value >= data.Length
                || string.IsNullOrWhiteSpace(data[options.AliasIndex.Value]))
            {
                yield break;
            }

            var aliases = data[options.AliasIndex.Value]
                .Split(options.Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal);

            foreach (var alias in aliases)
            {
                if (string.Equals(alias, primary.ElementValue.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return new SanctionEntryFileRecord(alias, primary.Reference,
                    HashHelper.GetHash(sanctionEntrySourceId + alias + primary.Reference), primary.Payload);
            }
        }

        private static SanctionEntryFileRecord BuildRecord(string[] data, int sanctionEntrySourceId,
            string multiPartStringIndex, int? referenceIndex)
        {
            if (referenceIndex == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            var first = true;

            foreach (var location in multiPartStringIndex.Split(','))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(' ');
                }

                sb.Append(int.TryParse(location, out var parsedIndex) ? data[parsedIndex] : data[0]);
            }

            var elementValue = sb.ToString();
            var reference = data[referenceIndex.Value];
            var hash = HashHelper.GetHash(sanctionEntrySourceId + elementValue + reference);

            return new SanctionEntryFileRecord(elementValue, reference, hash, string.Join(',', data));
        }
    }
}
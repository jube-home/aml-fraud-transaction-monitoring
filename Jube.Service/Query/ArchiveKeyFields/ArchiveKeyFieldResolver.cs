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

using System.Globalization;
using Jube.Data.Context;
using Jube.Data.Query.ArchiveKeyLookup;
using Jube.Data.Query.GetArchiveKeyFieldsByEntityAnalysisModelIdQuery;
using Jube.Dto.Options;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Service.Query.ArchiveKeyFields
{
    public sealed class ArchiveKeyFieldResolver(DbContext dbContext, Func<string, string>? encryptDeterministic = null)
    {
        private readonly Dictionary<int, IReadOnlyList<ArchiveKeyField>> cache = new();

        public async Task<IReadOnlyList<ArchiveKeyField>> FieldsAsync(int entityAnalysisModelId,
            CancellationToken token = default)
        {
            if (cache.TryGetValue(entityAnalysisModelId, out var cached))
            {
                return cached;
            }

            var fields = await new GetArchiveKeyFieldsByEntityAnalysisModelIdQuery(dbContext)
                .ExecuteAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            cache[entityAnalysisModelId] = fields;

            return fields;
        }

        public async Task<ArchiveKeyResolution> ResolveAcrossTenantAsync(int tenantRegistryId, string? key,
            string? value, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                return new ArchiveKeyResolution(ArchiveKeyResolutionState.NotArchiveKey, null, null);
            }

            var modelIds = await dbContext.EntityAnalysisModelRequestXpath
                .Where(x => x.Name == key && x.ReportTable == 1 && x.Active == 1 && (x.Deleted == null || x.Deleted == 0)
                            && x.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && (x.EntityAnalysisModel.Deleted == null || x.EntityAnalysisModel.Deleted == 0))
                .OrderBy(x => x.Id).Select(x => x.EntityAnalysisModelId).ToListAsync(token).ConfigureAwait(false);

            foreach (var modelId in modelIds.Where(id => id.HasValue))
            {
                var resolution = await ResolveAsync(modelId!.Value, key, value, token).ConfigureAwait(false);

                if (resolution.State != ArchiveKeyResolutionState.NotArchiveKey)
                {
                    return resolution;
                }
            }

            return new ArchiveKeyResolution(ArchiveKeyResolutionState.NotArchiveKey, null, null);
        }

        public async Task<ArchiveKeyField?> FindAsync(int entityAnalysisModelId, string? key,
            CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return (await FieldsAsync(entityAnalysisModelId, token).ConfigureAwait(false))
                .FirstOrDefault(f => string.Equals(f.Name, key, StringComparison.Ordinal));
        }

        public async Task<ArchiveKeyResolution> ResolveAsync(int entityAnalysisModelId, string? key, string? value,
            CancellationToken token = default, ArchiveKeyLookupOperator op = ArchiveKeyLookupOperator.Equal,
            string? upper = null)
        {
            var field = await FindAsync(entityAnalysisModelId, key, token).ConfigureAwait(false);

            if (field is null || !field.IsArchiveKey)
            {
                return new ArchiveKeyResolution(ArchiveKeyResolutionState.NotArchiveKey, field, null);
            }

            return Resolve(field, value, op, upper);
        }

        public ArchiveKeyResolution Resolve(ArchiveKeyField field, string? value,
            ArchiveKeyLookupOperator op = ArchiveKeyLookupOperator.Equal, string? upper = null)
        {
            if (!field.IsArchiveKey)
            {
                return new ArchiveKeyResolution(ArchiveKeyResolutionState.NotArchiveKey, field, null);
            }

            if (field.EncryptionId is 2 || (field.EncryptionId is 1 && encryptDeterministic is null))
            {
                return new ArchiveKeyResolution(ArchiveKeyResolutionState.NotLookupable, field, null);
            }

            if (op != ArchiveKeyLookupOperator.Equal && !field.AllowsRange)
            {
                return new ArchiveKeyResolution(ArchiveKeyResolutionState.OperatorNotAllowed, field, null);
            }

            if (!TryParse(field, value, out var parsed))
            {
                return new ArchiveKeyResolution(ArchiveKeyResolutionState.ValueInvalid, field, null);
            }

            object? parsedUpper = null;

            if (op == ArchiveKeyLookupOperator.Between && !TryParse(field, upper, out parsedUpper))
            {
                return new ArchiveKeyResolution(ArchiveKeyResolutionState.ValueInvalid, field, null);
            }

            var predicate = field.IsEntryKey
                ? ArchiveKeyPredicate.ForEntryKey((string)parsed!)
                : new ArchiveKeyPredicate(field.Name, field.ProcessingTypeId, field.Column, op, parsed!, parsedUpper);

            return new ArchiveKeyResolution(ArchiveKeyResolutionState.Resolved, field, predicate);
        }

        public bool TryParse(ArchiveKeyField field, string? text, out object? value)
        {
            value = null;

            if (text is null)
            {
                return false;
            }

            switch (field.Column)
            {
                case ArchiveKeyValueColumn.Integer:
                    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
                    {
                        return false;
                    }

                    value = integer;
                    return true;
                case ArchiveKeyValueColumn.Long:
                    if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                    {
                        return false;
                    }

                    value = longValue;
                    return true;
                case ArchiveKeyValueColumn.Float:
                    if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                        || !double.IsFinite(number))
                    {
                        return false;
                    }

                    value = number;
                    return true;
                case ArchiveKeyValueColumn.Boolean:
                    if (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1")
                    {
                        value = (short)1;
                        return true;
                    }

                    if (text.Equals("false", StringComparison.OrdinalIgnoreCase) || text == "0")
                    {
                        value = (short)0;
                        return true;
                    }

                    return false;
                case ArchiveKeyValueColumn.Date:
                    if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
                    {
                        return false;
                    }

                    value = DateTime.SpecifyKind(date.UtcDateTime, DateTimeKind.Unspecified);
                    return true;
                default:
                    if (text.Length == 0)
                    {
                        return false;
                    }

                    value = field.EncryptionId is 1 && encryptDeterministic is not null
                        ? encryptDeterministic(text)
                        : text;
                    return true;
            }
        }

        public static Func<string, string>? DeterministicEncryptor(DynamicEnvironment.DynamicEnvironment? environment)
        {
            var key = environment?.AppSettings("ElementSymmetricEncryptionKey");

            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var aes = new Cryptography.AesEncryption(key, Cryptography.IvMode.Deterministic);

            return text => aes.Encrypt(text, Cryptography.IvMode.Deterministic);
        }
    }
}

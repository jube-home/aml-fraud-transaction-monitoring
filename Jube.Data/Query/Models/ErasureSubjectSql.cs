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

namespace Jube.Data.Query.Models
{
    public static class ErasureSubjectSql
    {
        public const string SubjectEntries = """
            SELECT DISTINCT a."EntityAnalysisModelInstanceEntryGuid" AS guid
            FROM "ArchiveKey" k
            JOIN "Archive" a ON a."EntityAnalysisModelInstanceEntryGuid" = k."EntityAnalysisModelInstanceEntryGuid"
            JOIN "EntityAnalysisModel" m ON m."Id" = a."EntityAnalysisModelId"
            WHERE k."Key" = @key
              AND m."TenantRegistryId" = @tenant
              AND (k."KeyValueString" = @value
                   OR (@long IS NOT NULL AND (k."KeyValueLong" = @long OR k."KeyValueInteger" = @long)))
            """;

        public static LinqToDB.Data.DataParameter[] Parameters(int tenantRegistryId, string keyName, string keyValue,
            params LinqToDB.Data.DataParameter[] extra)
        {
            var list = new System.Collections.Generic.List<LinqToDB.Data.DataParameter>
            {
                new("tenant", tenantRegistryId),
                new("key", keyName),
                new("value", keyValue),
                new("long", long.TryParse(keyValue, out var number) ? number : null, LinqToDB.DataType.Int64)
            };

            list.AddRange(extra);

            return list.ToArray();
        }
    }
}

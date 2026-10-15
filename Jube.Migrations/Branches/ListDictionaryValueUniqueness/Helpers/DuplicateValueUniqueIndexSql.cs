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

namespace Jube.Migrations.Branches.ListDictionaryValueUniqueness.Helpers
{
    using System.Linq;

    public static class DuplicateValueUniqueIndexSql
    {
        public static string IndexName(string table)
        {
            return $"UX_{table}_Live";
        }

        public static string Up(string table, string versionTable, string versionParentIdColumn,
            string parentColumn, string valueColumn, string parentTable, int entityApprovalKindId,
            string[] extraColumns)
        {
            var copiedColumns = new[] { parentColumn, valueColumn }
                .Concat(extraColumns)
                .Concat([
                    "CreatedDate", "CreatedUser", "Deleted", "DeletedDate", "DeletedUser", "InheritedId", "Version",
                    "Guid", "DeleteExpiryDate"
                ])
                .ToArray();

            var quotedColumns = string.Join(", ", copiedColumns.Select(c => $"\"{c}\""));

            return $"""
                    WITH ranked AS (
                        SELECT "Id",
                               row_number() OVER (
                                   PARTITION BY "{parentColumn}", "{valueColumn}"
                                   ORDER BY (CASE WHEN "DeleteExpiryDate" IS NULL
                                                       OR "DeleteExpiryDate" > (now() AT TIME ZONE 'utc')
                                                  THEN 0 ELSE 1 END),
                                            "Id" DESC
                               ) AS rn
                        FROM "{table}"
                        WHERE COALESCE("Deleted", 0) = 0 AND "{valueColumn}" IS NOT NULL
                    ),
                    losers AS (
                        SELECT "Id" FROM ranked WHERE rn > 1
                    ),
                    pre_delete_snapshot AS (
                        INSERT INTO "{versionTable}" ("{versionParentIdColumn}", {quotedColumns})
                        SELECT "Id", {quotedColumns}
                        FROM "{table}"
                        WHERE "Id" IN (SELECT "Id" FROM losers)
                    ),
                    updated AS (
                        UPDATE "{table}"
                        SET "Deleted" = 1,
                            "DeletedDate" = (now() AT TIME ZONE 'utc'),
                            "DeletedUser" = 'Administrator',
                            "Version" = COALESCE("Version", 1) + 1
                        WHERE "Id" IN (SELECT "Id" FROM losers)
                        RETURNING "Id", {quotedColumns}
                    ),
                    post_delete_snapshot AS (
                        INSERT INTO "{versionTable}" ("{versionParentIdColumn}", {quotedColumns})
                        SELECT "Id", {quotedColumns}
                        FROM updated
                    )
                    INSERT INTO "EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId",
                        "EntityVersion", "StateId", "CreatedDate", "CreatedUser", "Guid")
                    SELECT m."TenantRegistryId", {entityApprovalKindId}, u."Id", u."Version", 1,
                           (now() AT TIME ZONE 'utc'), 'Administrator', gen_random_uuid()
                    FROM updated u
                    JOIN "{parentTable}" p ON p."Id" = u."{parentColumn}"
                    JOIN "EntityAnalysisModel" m ON m."Guid" = p."EntityAnalysisModelGuid"
                    WHERE m."TenantRegistryId" IS NOT NULL;

                    CREATE UNIQUE INDEX IF NOT EXISTS "{IndexName(table)}"
                        ON "{table}" ("{parentColumn}", "{valueColumn}")
                        WHERE COALESCE("Deleted", 0) = 0;
                    """;
        }

        public static string Down(string table)
        {
            return $"DROP INDEX IF EXISTS \"{IndexName(table)}\";";
        }
    }
}
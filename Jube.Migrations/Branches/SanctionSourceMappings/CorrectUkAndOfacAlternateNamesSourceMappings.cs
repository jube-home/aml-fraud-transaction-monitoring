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

namespace Jube.Migrations.Branches.SanctionSourceMappings
{
    using FluentMigrator;

    [Migration(2026111013000000)]
    public class CorrectUkAndOfacAlternateNamesSourceMappings : Migration
    {
        private const string SoftDeleteStaleEntries = """
UPDATE "SanctionEntry"
SET "Deleted" = 1, "DeletedDate" = timezone('utc', now()), "DeletedUser" = 'Migration'
WHERE ("Deleted" IS NULL OR "Deleted" = 0)
  AND "SanctionEntrySourceId" IN (
      SELECT "Id" FROM "SanctionEntrySource"
      WHERE ("Name" = 'BOE' AND "MultiPartStringIndex" = '0,1,2,3,4,5' AND "ReferenceIndex" = 28)
         OR ("Name" = 'SDN ALT' AND "Delimiter" = ';' AND "ReferenceIndex" = 8));
""";

        private const string CorrectBoe = """
UPDATE "SanctionEntrySource"
SET "Delimiter" = ',', "MultiPartStringIndex" = '4,5,6,7,8,9', "ReferenceIndex" = 1, "Skip" = 2
WHERE "Name" = 'BOE' AND "MultiPartStringIndex" = '0,1,2,3,4,5' AND "ReferenceIndex" = 28;
""";

        private const string CorrectSdnAlternateNames = """
UPDATE "SanctionEntrySource"
SET "Delimiter" = ',', "MultiPartStringIndex" = '3', "ReferenceIndex" = 0, "Skip" = 0
WHERE "Name" = 'SDN ALT' AND "Delimiter" = ';' AND "ReferenceIndex" = 8;
""";

        public override void Up()
        {
            Execute.Sql(SoftDeleteStaleEntries);
            Execute.Sql(CorrectBoe);
            Execute.Sql(CorrectSdnAlternateNames);
        }

        public override void Down()
        {
        }
    }
}

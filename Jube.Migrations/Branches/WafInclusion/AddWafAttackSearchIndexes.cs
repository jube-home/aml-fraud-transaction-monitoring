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

namespace Jube.Migrations.Branches.WafInclusion
{
    using FluentMigrator;

    [Migration(20260924100400)]
    public class AddWafAttackSearchIndexes : Migration
    {
        private static readonly string[] trigramColumns =
            ["MatchedValue", "Route", "SignatureName", "Category", "MatchedField"];

        private static readonly string[] filterColumns =
            ["RemoteIp", "UserName", "Action", "SignatureName", "Category", "Transport"];

        public override void Up()
        {
            Execute.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            foreach (var column in trigramColumns)
            {
                Execute.Sql(
                    $"CREATE INDEX IF NOT EXISTS \"IX_WafAttack_{column}_Trgm\" ON \"WafAttack\" USING gin (lower(\"{column}\") gin_trgm_ops);");
            }

            foreach (var column in filterColumns)
            {
                Execute.Sql(
                    $"CREATE INDEX IF NOT EXISTS \"IX_WafAttack_{column}\" ON \"WafAttack\" (\"{column}\");");
            }
        }

        public override void Down()
        {
        }
    }
}
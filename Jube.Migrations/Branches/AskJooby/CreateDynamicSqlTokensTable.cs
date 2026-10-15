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

using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20260917160000)]
    public class CreateDynamicSqlTokensTable : Migration
    {
        public override void Up()
        {
            Create.Table("DynamicSqlTokens")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TableName").AsString(255).NotNullable()
                .WithColumn("RequiredPermissions").AsCustom("integer[]").NotNullable()
                .WithColumn("Active").AsByte().NotNullable().WithDefaultValue(1)
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable();

            Create.Index().OnTable("DynamicSqlTokens").OnColumn("TableName").Unique();

            foreach (var tableName in new[] { "Case", "CaseNote", "CaseFile", "CaseEvent" })
            {
                Execute.Sql("INSERT INTO \"DynamicSqlTokens\" " +
                            "(\"TableName\", \"RequiredPermissions\", \"Active\", \"CreatedDate\", \"CreatedUser\") " +
                            "VALUES ('" + tableName + "', '{1}', 1, NOW(), 'Administrator');");
            }
        }

        public override void Down()
        {
        }
    }
}
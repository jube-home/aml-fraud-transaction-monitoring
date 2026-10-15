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
    [Migration(20260917130000)]
    public class CreateDynamicSqlQueryLogTable : Migration
    {
        public override void Up()
        {
            Create.Table("DynamicSqlQueryLog")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable()
                .WithColumn("Sql").AsString(int.MaxValue).Nullable()
                .WithColumn("Succeeded").AsBoolean().NotNullable()
                .WithColumn("RowCount").AsInt32().Nullable()
                .WithColumn("DurationMs").AsInt64().NotNullable()
                .WithColumn("ErrorMessage").AsString(int.MaxValue).Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable();

            Create.Index().OnTable("DynamicSqlQueryLog").OnColumn("AskJoobyConversationId").Ascending();
            Create.Index().OnTable("DynamicSqlQueryLog").OnColumn("CreatedUser").Ascending();
            Create.Index().OnTable("DynamicSqlQueryLog").OnColumn("CreatedDate").Ascending();

            Create.ForeignKey().FromTable("DynamicSqlQueryLog").ForeignColumn("AskJoobyConversationId")
                .ToTable("AskJoobyConversation").PrimaryColumn("Id");
        }

        public override void Down()
        {
        }
    }
}
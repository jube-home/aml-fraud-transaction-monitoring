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
    [Migration(20260917120000)]
    public class CreateAskJoobyDataCacheTable : Migration
    {
        public override void Up()
        {
            Create.Table("AskJoobyDataCache")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("PointerKey").AsString(64).NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable()
                .WithColumn("Label").AsString(255).Nullable()
                .WithColumn("ColumnsJson").AsCustom("jsonb").NotNullable()
                .WithColumn("RowCount").AsInt32().NotNullable()
                .WithColumn("DataJson").AsCustom("jsonb").NotNullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("ExpiresAt").AsDateTime2().Nullable();

            Create.Index().OnTable("AskJoobyDataCache").OnColumn("PointerKey").Unique();
            Create.Index().OnTable("AskJoobyDataCache").OnColumn("TenantRegistryId").Ascending();
            Create.Index().OnTable("AskJoobyDataCache").OnColumn("AskJoobyConversationId").Ascending();
            Create.Index().OnTable("AskJoobyDataCache").OnColumn("ExpiresAt").Ascending();

            Create.ForeignKey().FromTable("AskJoobyDataCache").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.ForeignKey().FromTable("AskJoobyDataCache").ForeignColumn("AskJoobyConversationId")
                .ToTable("AskJoobyConversation").PrimaryColumn("Id");
        }

        public override void Down()
        {
        }
    }
}
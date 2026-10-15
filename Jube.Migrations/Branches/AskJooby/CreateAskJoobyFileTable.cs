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
    [Migration(20260916092502)]
    public class CreateAskJoobyFileTable : Migration
    {
        public override void Up()
        {
            Create.Table("AskJoobyFile")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AskJoobyConversationId").AsInt32().Nullable()
                .WithColumn("Object").AsBinary().Nullable()
                .WithColumn("Name").AsString(255).Nullable()
                .WithColumn("ContentType").AsString(255).Nullable()
                .WithColumn("Extension").AsString(32).Nullable()
                .WithColumn("Size").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString(255).Nullable()
                .WithColumn("Deleted").AsByte().Nullable();

            Create.Index().OnTable("AskJoobyFile").OnColumn("AskJoobyConversationId").Ascending();

            Create.ForeignKey().FromTable("AskJoobyFile").ForeignColumn("AskJoobyConversationId")
                .ToTable("AskJoobyConversation").PrimaryColumn("Id");
        }

        public override void Down()
        {
        }
    }
}
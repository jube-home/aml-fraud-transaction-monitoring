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

    [Migration(20260924100200)]
    public class AddTableWafException : Migration
    {
        public override void Up()
        {
            Create.Table("WafException")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("RouteRegex").AsCustom("text").Nullable()
                .WithColumn("FieldRegex").AsCustom("text").Nullable()
                .WithColumn("WafSignatureId").AsInt32().Nullable()
                .WithColumn("Note").AsCustom("text").Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.Index().OnTable("WafException")
                .OnColumn("Active").Ascending();
        }

        public override void Down()
        {
        }
    }
}
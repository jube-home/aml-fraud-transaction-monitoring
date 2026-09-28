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

    [Migration(20260924100300)]
    public class AddTableWafAttack : Migration
    {
        public override void Up()
        {
            Create.Table("WafAttack")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("Transport").AsString().Nullable()
                .WithColumn("Route").AsCustom("text").Nullable()
                .WithColumn("Method").AsString().Nullable()
                .WithColumn("RemoteIp").AsString().Nullable()
                .WithColumn("UserName").AsString().Nullable()
                .WithColumn("WafSignatureId").AsInt32().Nullable()
                .WithColumn("SignatureName").AsString().Nullable()
                .WithColumn("Category").AsString().Nullable()
                .WithColumn("MatchedField").AsCustom("text").Nullable()
                .WithColumn("MatchedValue").AsCustom("text").Nullable()
                .WithColumn("Action").AsString().Nullable()
                .WithColumn("CorrelationId").AsString().Nullable()
                .WithColumn("Instance").AsString().Nullable();

            Create.Index().OnTable("WafAttack")
                .OnColumn("CreatedDate").Ascending();
        }

        public override void Down()
        {
        }
    }
}
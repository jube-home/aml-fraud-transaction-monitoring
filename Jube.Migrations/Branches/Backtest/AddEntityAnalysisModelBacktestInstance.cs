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

namespace Jube.Migrations.Branches.Backtest
{
    using System;
    using FluentMigrator;

    [Migration(20263107410000)]
    public class AddEntityAnalysisModelBacktestInstance : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelBacktestInstance")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().NotNullable()
                .WithColumn("RuleType").AsString().NotNullable()
                .WithColumn("RuleId").AsInt32().Nullable()
                .WithColumn("Request").AsCustom("jsonb").NotNullable()
                .WithColumn("Status").AsInt16().NotNullable()
                .WithColumn("Scanned").AsInt64().NotNullable().WithDefaultValue(0)
                .WithColumn("Evaluated").AsInt64().NotNullable().WithDefaultValue(0)
                .WithColumn("Progress").AsDouble().NotNullable().WithDefaultValue(0)
                .WithColumn("Result").AsCustom("jsonb").Nullable()
                .WithColumn("Error").AsString(int.MaxValue).Nullable()
                .WithColumn("CreatedDate").AsDateTime2().NotNullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("StartedDate").AsDateTime2().Nullable()
                .WithColumn("CompletedDate").AsDateTime2().Nullable()
                .WithColumn("HeartbeatDate").AsDateTime2().Nullable()
                .WithColumn("ClaimedBy").AsString().Nullable()
                .WithColumn("ClaimedDate").AsDateTime2().Nullable()
                .WithColumn("Deleted").AsInt16().NotNullable().WithDefaultValue(0)
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable();

            Create.Index().OnTable("EntityAnalysisModelBacktestInstance")
                .OnColumn("Status").Ascending()
                .OnColumn("CreatedDate").Ascending();

            Create.Index()
                .OnTable("EntityAnalysisModelBacktestInstance")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("RuleType").Ascending()
                .OnColumn("RuleId").Ascending();

            Create.Index()
                .OnTable("EntityAnalysisModelBacktestInstance")
                .OnColumn("Guid").Ascending()
                .WithOptions().Unique();

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 61,
                Name = "Run Backtest"
            });

            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 61,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });
        }

        public override void Down()
        {
        }
    }
}
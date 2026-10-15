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

using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20263107520050)]
    public class AddAdvancedAnalyticsRScriptRun : Migration
    {
        public override void Up()
        {
            Create.Table("AdvancedAnalyticsRScriptRun")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("UserName").AsString(int.MaxValue).Nullable()
                .WithColumn("Status").AsInt32().NotNullable()
                .WithColumn("Script").AsString(int.MaxValue).NotNullable()
                .WithColumn("TimeoutSeconds").AsInt32().Nullable()
                .WithColumn("ConsoleOutput").AsString(int.MaxValue).Nullable()
                .WithColumn("ResultJson").AsCustom("jsonb").Nullable()
                .WithColumn("Warnings").AsCustom("jsonb").Nullable()
                .WithColumn("DurationMs").AsDouble().Nullable()
                .WithColumn("RVersion").AsString(int.MaxValue).Nullable()
                .WithColumn("Error").AsString(int.MaxValue).Nullable()
                .WithColumn("EngineName").AsString(int.MaxValue).Nullable()
                .WithColumn("HeartbeatDate").AsDateTime().Nullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("StartedDate").AsDateTime().Nullable()
                .WithColumn("CompletedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(int.MaxValue).Nullable();

            Create.UniqueConstraint("UQ_AdvancedAnalyticsRScriptRun_Guid")
                .OnTable("AdvancedAnalyticsRScriptRun").Column("Guid");

            Create.ForeignKey("FK_AdvancedAnalyticsRScriptRun_TenantRegistry")
                .FromTable("AdvancedAnalyticsRScriptRun").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.Index("IX_AdvancedAnalyticsRScriptRun_Tenant_Status")
                .OnTable("AdvancedAnalyticsRScriptRun")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("Status").Ascending();

            Create.Index("IX_AdvancedAnalyticsRScriptRun_Tenant_Created")
                .OnTable("AdvancedAnalyticsRScriptRun")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("CreatedDate").Descending();

            Execute.Sql("""
                        CREATE INDEX "IX_AdvancedAnalyticsRScriptRun_Pending"
                        ON "AdvancedAnalyticsRScriptRun" ("CreatedDate", "Id")
                        WHERE "Status" = 1
                        """);

            Execute.Sql("""
                        CREATE INDEX "IX_AdvancedAnalyticsRScriptRun_Active"
                        ON "AdvancedAnalyticsRScriptRun" ("TenantRegistryId", "HeartbeatDate")
                        WHERE "Status" IN (2, 5)
                        """);
        }

        public override void Down()
        {
        }
    }
}
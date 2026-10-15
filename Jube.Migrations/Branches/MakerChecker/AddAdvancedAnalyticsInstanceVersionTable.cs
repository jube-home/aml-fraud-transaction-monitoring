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

namespace Jube.Migrations.Branches.MakerChecker
{
    using FluentMigrator;

    [Migration(20260929120500)]
    public class AddAdvancedAnalyticsInstanceVersionTable : Migration
    {
        public override void Up()
        {
            Create.Table("AdvancedAnalyticsInstanceVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("AdvancedAnalyticsInstanceId").AsInt32().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString(int.MaxValue).Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("CreatedUser").AsString(int.MaxValue).Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString(int.MaxValue).Nullable();

            Create.ForeignKey("FK_AdvancedAnalyticsInstanceVersion_AdvancedAnalyticsInstance")
                .FromTable("AdvancedAnalyticsInstanceVersion").ForeignColumn("AdvancedAnalyticsInstanceId")
                .ToTable("AdvancedAnalyticsInstance").PrimaryColumn("Id");

            Create.Index("IX_AdvancedAnalyticsInstanceVersion_InstanceId")
                .OnTable("AdvancedAnalyticsInstanceVersion").OnColumn("AdvancedAnalyticsInstanceId");
        }

        public override void Down()
        {
        }
    }
}
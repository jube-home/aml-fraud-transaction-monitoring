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
    [Migration(20263107270100)]
    public class AddAdvancedAnalyticsTrainingRunQueueColumns : Migration
    {
        public override void Up()
        {
            Alter.Table("AdvancedAnalyticsTrainingRun")
                .AddColumn("HeartbeatDate").AsDateTime().Nullable()
                .AddColumn("ClaimedBy").AsString(int.MaxValue).Nullable()
                .AddColumn("ClaimedDate").AsDateTime().Nullable();

            Execute.Sql("""
                        CREATE INDEX "IX_AdvancedAnalyticsTrainingRun_Pending"
                        ON "AdvancedAnalyticsTrainingRun" ("CreatedDate", "Id")
                        WHERE "StatusId" = 1 AND "Deleted" = 0
                        """);

            Execute.Sql("""
                        CREATE INDEX "IX_AdvancedAnalyticsTrainingRun_Active"
                        ON "AdvancedAnalyticsTrainingRun" ("TenantRegistryId", "HeartbeatDate")
                        WHERE "StatusId" IN (2, 5) AND "Deleted" = 0
                        """);

            Create.Index("IX_AdvancedAnalyticsTrainingRun_Tenant_Started")
                .OnTable("AdvancedAnalyticsTrainingRun")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("StartedDate").Descending();
        }

        public override void Down()
        {
        }
    }
}
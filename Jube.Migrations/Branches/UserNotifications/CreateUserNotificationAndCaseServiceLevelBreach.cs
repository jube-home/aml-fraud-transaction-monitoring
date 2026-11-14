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

namespace Jube.Migrations.Branches.UserNotifications
{
    using FluentMigrator;

    [Migration(2026111016000000)]
    public class CreateUserNotificationAndCaseServiceLevelBreach : Migration
    {
        public override void Up()
        {
            Create.Table("UserNotification")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("RecipientUser").AsString().NotNullable()
                .WithColumn("KindId").AsInt16().NotNullable()
                .WithColumn("PriorityId").AsInt16().NotNullable()
                .WithColumn("CaseId").AsInt32().Nullable()
                .WithColumn("ArgumentsJson").AsCustom("jsonb").Nullable()
                .WithColumn("ActorUser").AsString().Nullable()
                .WithColumn("DedupeKey").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("ReadDate").AsDateTime().Nullable()
                .WithColumn("ResolvedDate").AsDateTime().Nullable()
                .WithColumn("DismissedDate").AsDateTime().Nullable();

            Create.Index("IX_UserNotification_Recipient")
                .OnTable("UserNotification")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("RecipientUser").Ascending()
                .OnColumn("ResolvedDate").Ascending()
                .OnColumn("DismissedDate").Ascending();

            Create.Index("IX_UserNotification_Guid")
                .OnTable("UserNotification")
                .OnColumn("Guid").Ascending()
                .WithOptions().Unique();

            Create.Table("CaseServiceLevelBreach")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("DimensionId").AsInt16().NotNullable()
                .WithColumn("BreachedDate").AsDateTime().NotNullable();

            Create.Index("IX_CaseServiceLevelBreach_CaseDimension")
                .OnTable("CaseServiceLevelBreach")
                .OnColumn("CaseId").Ascending()
                .OnColumn("DimensionId").Ascending()
                .WithOptions().Unique();

            Execute.Sql("""
                        CREATE UNIQUE INDEX "IX_UserNotification_Dedupe" ON public."UserNotification" ("TenantRegistryId", "RecipientUser", "DedupeKey") WHERE "ResolvedDate" IS NULL AND "DismissedDate" IS NULL;
                        COMMENT ON TABLE public."UserNotification" IS 'An in-app notification for one user. Persisted by the platform (for example when a case breaches a service level) and read by the notification bell. A user only ever reads their own rows.';
                        COMMENT ON COLUMN public."UserNotification"."Guid" IS 'The identifier the client uses; the database id never leaves the server.';
                        COMMENT ON COLUMN public."UserNotification"."RecipientUser" IS 'The user name the notification is for, matching UserRegistry.Name and Case.LockedUser.';
                        COMMENT ON COLUMN public."UserNotification"."KindId" IS 'Enumeration (NotificationKind) of what happened: 1 Case service level breached.';
                        COMMENT ON COLUMN public."UserNotification"."PriorityId" IS 'Enumeration (NotificationPriority): 1 Low, 2 Normal, 3 High.';
                        COMMENT ON COLUMN public."UserNotification"."CaseId" IS 'The case the notification is about, when it is about one. Rows for a case the recipient can no longer see are not returned.';
                        COMMENT ON COLUMN public."UserNotification"."ArgumentsJson" IS 'Structured facts the client renders into text, for example the service level dimension. The server stores no prose.';
                        COMMENT ON COLUMN public."UserNotification"."ActorUser" IS 'The user who caused the notification, null when the platform did.';
                        COMMENT ON COLUMN public."UserNotification"."DedupeKey" IS 'Makes publishing idempotent: only one unresolved, undismissed row per recipient and key exists.';
                        COMMENT ON COLUMN public."UserNotification"."ReadDate" IS 'When the recipient opened the notification. Null while unread.';
                        COMMENT ON COLUMN public."UserNotification"."ResolvedDate" IS 'When the notification stopped being relevant. Resolved rows drop out of the active list.';
                        COMMENT ON COLUMN public."UserNotification"."DismissedDate" IS 'When the recipient cleared the notification.';
                        COMMENT ON TABLE public."CaseServiceLevelBreach" IS 'One row per case and service level dimension that has been found in breach, so the breach is recorded and notified once however many sweeps see it.';
                        COMMENT ON COLUMN public."CaseServiceLevelBreach"."DimensionId" IS 'Enumeration (ServiceLevelDimension): 1 First view, 2 Ownership, 3 Resolution, 4 Status dwell.';
                        COMMENT ON COLUMN public."CaseServiceLevelBreach"."BreachedDate" IS 'When the sweep first found the breach (UTC).';
                        """);
        }

        public override void Down()
        {
        }
    }
}

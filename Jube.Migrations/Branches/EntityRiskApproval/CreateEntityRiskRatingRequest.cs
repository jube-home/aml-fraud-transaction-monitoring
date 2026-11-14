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

namespace Jube.Migrations.Branches.EntityRiskApproval
{
    using FluentMigrator;

    [Migration(2026111110000000)]
    public class CreateEntityRiskRatingRequest : Migration
    {
        public override void Up()
        {
            Create.Table("EntityRiskRatingRequest")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable().Unique()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("KeyName").AsString(128).NotNullable()
                .WithColumn("KeyValue").AsString(256).NotNullable()
                .WithColumn("TierId").AsInt16().NotNullable()
                .WithColumn("Score").AsDouble().Nullable()
                .WithColumn("ReviewByDate").AsDateTime().Nullable()
                .WithColumn("Reason").AsString(500).NotNullable()
                .WithColumn("BasedOnVersion").AsInt32().NotNullable()
                .WithColumn("StateId").AsInt16().NotNullable()
                .WithColumn("ResultingVersion").AsInt32().Nullable()
                .WithColumn("RequestedUser").AsString().NotNullable()
                .WithColumn("RequestedDate").AsDateTime().NotNullable()
                .WithColumn("DecidedUser").AsString().Nullable()
                .WithColumn("DecidedDate").AsDateTime().Nullable()
                .WithColumn("DecisionNote").AsString(500).Nullable();

            Create.Index("IX_EntityRiskRatingRequest_State").OnTable("EntityRiskRatingRequest")
                .OnColumn("TenantRegistryId").Ascending().OnColumn("StateId").Ascending();

            Execute.Sql("""
                        INSERT INTO public."PermissionSpecification" ("Id", "Name") VALUES (80, 'Approve Entity Risk Ratings');
                        INSERT INTO public."RoleRegistryPermission" ("RoleRegistryId", "PermissionSpecificationId", "Active", "CreatedDate", "CreatedUser", "Version", "Guid")
                        SELECT 1, s."Id", 1, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid()
                        FROM public."PermissionSpecification" s WHERE s."Id" = 80
                          AND NOT EXISTS (SELECT 1 FROM public."RoleRegistryPermission" x WHERE x."RoleRegistryId" = 1
                              AND x."PermissionSpecificationId" = s."Id" AND (x."Deleted" = 0 OR x."Deleted" IS NULL));

                        COMMENT ON TABLE public."EntityRiskRatingRequest" IS 'A proposed manual change to an entity risk rating. A second person approves it, and only then is a new EntityRiskRating version written. Imports and computed ratings do not need approval.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."TenantRegistryId" IS 'The tenant the rating belongs to.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."KeyName" IS 'The key field name of the entity, joins EntityRiskRating on KeyName.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."KeyValue" IS 'The key value of the entity, joins EntityRiskRating on KeyValue.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."TierId" IS 'Enumeration (EntityRiskTier) proposed: 1 Low, 2 Medium, 3 High.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."Score" IS 'The score proposed alongside the tier.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."ReviewByDate" IS 'UTC. The review date proposed.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."Reason" IS 'Why the change is proposed. Required.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."BasedOnVersion" IS 'The current rating version when the change was proposed, 0 when none. Approval is refused if the rating has changed since.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."StateId" IS 'Enumeration (EntityRiskRequestState): 1 Pending, 2 Approved, 3 Rejected, 4 Retracted.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."ResultingVersion" IS 'The EntityRiskRating version written on approval.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."RequestedUser" IS 'Who proposed it. Never the approver.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."RequestedDate" IS 'UTC. When it was proposed.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."DecidedUser" IS 'Who approved, rejected or retracted it.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."DecidedDate" IS 'UTC. When it was decided.';
                        COMMENT ON COLUMN public."EntityRiskRatingRequest"."DecisionNote" IS 'The reason given with the decision.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

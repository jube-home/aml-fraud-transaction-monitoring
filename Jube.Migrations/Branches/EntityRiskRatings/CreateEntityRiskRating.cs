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

namespace Jube.Migrations.Branches.EntityRiskRatings
{
    using FluentMigrator;

    [Migration(2026111101000000)]
    public class CreateEntityRiskRating : Migration
    {
        public override void Up()
        {
            Create.Table("EntityRiskRating")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("KeyName").AsString(128).NotNullable()
                .WithColumn("KeyValue").AsString(256).NotNullable()
                .WithColumn("Version").AsInt32().NotNullable()
                .WithColumn("TierId").AsInt16().NotNullable()
                .WithColumn("Score").AsDouble().Nullable()
                .WithColumn("SourceId").AsInt16().NotNullable()
                .WithColumn("Reason").AsString(500).Nullable()
                .WithColumn("ReviewByDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Index("IX_EntityRiskRating_Version")
                .OnTable("EntityRiskRating")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("KeyName").Ascending()
                .OnColumn("KeyValue").Ascending()
                .OnColumn("Version").Ascending()
                .WithOptions().Unique();

            Execute.Sql("""
                        COMMENT ON TABLE public."EntityRiskRating" IS 'The risk rating of an entity key (for example an account or customer id), as supplied by the institution or set by hand. Append only: each change is a new version and the highest version is the current rating, so the rating history is the table itself.';
                        COMMENT ON COLUMN public."EntityRiskRating"."TenantRegistryId" IS 'The tenant the rating belongs to.';
                        COMMENT ON COLUMN public."EntityRiskRating"."KeyName" IS 'The name of the key field the rating is about, for example Account.';
                        COMMENT ON COLUMN public."EntityRiskRating"."KeyValue" IS 'The value of the key exactly as it is stored against transactions and cases, so ciphertext for an encrypted key.';
                        COMMENT ON COLUMN public."EntityRiskRating"."Version" IS 'Starts at 1 and rises by one with each change; the highest version is current.';
                        COMMENT ON COLUMN public."EntityRiskRating"."TierId" IS 'Enumeration (EntityRiskTier): 1 Low, 2 Medium, 3 High.';
                        COMMENT ON COLUMN public."EntityRiskRating"."Score" IS 'Optional numeric score the institution supplied alongside the tier.';
                        COMMENT ON COLUMN public."EntityRiskRating"."SourceId" IS 'Enumeration (EntityRiskSource): 1 Imported from the institution, 2 Set by hand.';
                        COMMENT ON COLUMN public."EntityRiskRating"."Reason" IS 'Why the rating was set; required when set by hand.';
                        COMMENT ON COLUMN public."EntityRiskRating"."ReviewByDate" IS 'When the rating is next due a review; a rating past this date is reported as due.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

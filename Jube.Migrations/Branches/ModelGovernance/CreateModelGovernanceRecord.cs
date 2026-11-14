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

namespace Jube.Migrations.Branches.ModelGovernance
{
    using FluentMigrator;

    [Migration(2026111111000000)]
    public class CreateModelGovernanceRecord : Migration
    {
        public override void Up()
        {
            Create.Table("ModelGovernanceRecord")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("KindId").AsInt16().NotNullable()
                .WithColumn("ModelGuid").AsGuid().NotNullable()
                .WithColumn("Version").AsInt32().NotNullable()
                .WithColumn("OwnerUser").AsString(200).NotNullable()
                .WithColumn("IntendedPurpose").AsString(2000).NotNullable()
                .WithColumn("RiskTierId").AsInt16().NotNullable()
                .WithColumn("HumanOversight").AsString(2000).NotNullable()
                .WithColumn("NextReviewDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Index("IX_ModelGovernanceRecord_Version").OnTable("ModelGovernanceRecord")
                .OnColumn("TenantRegistryId").Ascending().OnColumn("KindId").Ascending()
                .OnColumn("ModelGuid").Ascending().OnColumn("Version").Ascending().WithOptions().Unique();

            Execute.Sql("""
                        COMMENT ON TABLE public."ModelGovernanceRecord" IS 'The governance statement for one model: who owns it, what it is for, how risky it is and how a person oversees it. Append only: each save is a new version and the highest is current. Feeds the model inventory and the model card.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."TenantRegistryId" IS 'The tenant the model belongs to.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."KindId" IS 'Enumeration (ModelGovernanceKind): 1 Entity analysis model, 2 Exhaustive adaptation.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."ModelGuid" IS 'The Guid of the model the statement is about. Joins EntityAnalysisModel or ExhaustiveSearchInstance on Guid according to KindId.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."Version" IS 'Starts at 1 and rises with each save; the highest is current.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."OwnerUser" IS 'The person accountable for the model.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."IntendedPurpose" IS 'What the model is for and the decisions it informs.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."RiskTierId" IS 'Enumeration (ModelRiskTier): 1 Low, 2 Medium, 3 High.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."HumanOversight" IS 'How a person reviews or can override what the model does.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."NextReviewDate" IS 'UTC. When the model is next due a governance review.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."CreatedUser" IS 'Who saved this version.';
                        COMMENT ON COLUMN public."ModelGovernanceRecord"."CreatedDate" IS 'UTC. When this version was saved.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

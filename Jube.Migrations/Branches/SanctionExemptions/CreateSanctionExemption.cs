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

namespace Jube.Migrations.Branches.SanctionExemptions
{
    using FluentMigrator;

    [Migration(2026111020000000)]
    public class CreateSanctionExemption : Migration
    {
        public override void Up()
        {
            Create.Table("SanctionExemption")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("ScreenedName").AsString(500).NotNullable()
                .WithColumn("NameKey").AsString(500).NotNullable()
                .WithColumn("SanctionEntrySourceId").AsInt32().NotNullable()
                .WithColumn("SanctionEntryReference").AsString(500).NotNullable()
                .WithColumn("Reason").AsString(1000).NotNullable()
                .WithColumn("ExpiryDate").AsDateTime().Nullable()
                .WithColumn("StateId").AsInt16().NotNullable()
                .WithColumn("RequestedUser").AsString().NotNullable()
                .WithColumn("RequestedDate").AsDateTime().NotNullable()
                .WithColumn("DecidedUser").AsString().Nullable()
                .WithColumn("DecidedDate").AsDateTime().Nullable()
                .WithColumn("DecisionNote").AsString(1000).Nullable();

            Create.Index("IX_SanctionExemption_Tenant")
                .OnTable("SanctionExemption")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("StateId").Ascending();

            Execute.Sql("""
                        CREATE UNIQUE INDEX "IX_SanctionExemption_Live" ON public."SanctionExemption" ("TenantRegistryId", "NameKey", "SanctionEntrySourceId", "SanctionEntryReference") WHERE "StateId" IN (1, 2);
                        COMMENT ON TABLE public."SanctionExemption" IS 'A good-guy entry: a screened name that an analyst has judged is not the sanctioned entity it matched. Needs a second person to approve before it takes effect. While approved and unexpired, that entry is left out of the matches for that name in that tenant.';
                        COMMENT ON COLUMN public."SanctionExemption"."ScreenedName" IS 'The name as it was screened, kept for display.';
                        COMMENT ON COLUMN public."SanctionExemption"."NameKey" IS 'The screened name folded and tokenised the way the matcher does, tokens sorted, so spelling variants in case, punctuation and word order are the same key.';
                        COMMENT ON COLUMN public."SanctionExemption"."SanctionEntrySourceId" IS 'The sanction list (SanctionEntrySource.Id) the exempted entry belongs to.';
                        COMMENT ON COLUMN public."SanctionExemption"."SanctionEntryReference" IS 'The reference of the exempted entry within its list, for example the SDN number.';
                        COMMENT ON COLUMN public."SanctionExemption"."Reason" IS 'Why the match is judged not to be the sanctioned entity. Required.';
                        COMMENT ON COLUMN public."SanctionExemption"."ExpiryDate" IS 'When the exemption lapses (UTC). Null means it does not lapse.';
                        COMMENT ON COLUMN public."SanctionExemption"."StateId" IS 'Enumeration (SanctionExemptionState): 1 Pending approval, 2 Approved, 3 Rejected, 4 Retracted.';
                        COMMENT ON COLUMN public."SanctionExemption"."RequestedUser" IS 'Who asked for the exemption. The same person can never approve it.';
                        COMMENT ON COLUMN public."SanctionExemption"."DecidedUser" IS 'Who approved, rejected or retracted it.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

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

namespace Jube.Migrations.Branches.SubjectErasure
{
    using FluentMigrator;

    [Migration(2026111108000000)]
    public class CreateErasureRequest : Migration
    {
        public override void Up()
        {
            Create.Table("ErasureRequest")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable().Unique()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("KeyName").AsString(200).NotNullable()
                .WithColumn("KeyValue").AsString(500).NotNullable()
                .WithColumn("Reason").AsString(1000).NotNullable()
                .WithColumn("StateId").AsInt16().NotNullable()
                .WithColumn("RefusalReasonId").AsInt16().Nullable()
                .WithColumn("ArchiveRowsFound").AsInt32().Nullable()
                .WithColumn("ArchiveRowsRemoved").AsInt32().Nullable()
                .WithColumn("GraphNodesRemoved").AsInt32().Nullable()
                .WithColumn("GraphEdgesRemoved").AsInt32().Nullable()
                .WithColumn("CasePayloadsNulled").AsInt32().Nullable()
                .WithColumn("RequestedUser").AsString().NotNullable()
                .WithColumn("RequestedDate").AsDateTime().NotNullable()
                .WithColumn("DecidedUser").AsString().Nullable()
                .WithColumn("DecidedDate").AsDateTime().Nullable()
                .WithColumn("DecisionNote").AsString(1000).Nullable()
                .WithColumn("ExecutedDate").AsDateTime().Nullable();

            Create.Index("IX_ErasureRequest_State").OnTable("ErasureRequest").OnColumn("TenantRegistryId").Ascending()
                .OnColumn("StateId").Ascending();

            Execute.Sql("""
                        INSERT INTO public."PermissionSpecification" ("Id", "Name") VALUES (78, 'Request Erasure and Export Subject Data');
                        INSERT INTO public."PermissionSpecification" ("Id", "Name") VALUES (79, 'Approve Erasure');
                        INSERT INTO public."RoleRegistryPermission" ("RoleRegistryId", "PermissionSpecificationId", "Active", "CreatedDate", "CreatedUser", "Version", "Guid")
                        SELECT 1, s."Id", 1, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid()
                        FROM public."PermissionSpecification" s WHERE s."Id" IN (78, 79)
                          AND NOT EXISTS (SELECT 1 FROM public."RoleRegistryPermission" x WHERE x."RoleRegistryId" = 1
                              AND x."PermissionSpecificationId" = s."Id" AND (x."Deleted" = 0 OR x."Deleted" IS NULL));

                        COMMENT ON TABLE public."ErasureRequest" IS 'A governed request to erase everything held about one subject, identified by an archive key name and value. A second person approves it, and the engine then removes the archived transactions, their keys, tags and versions, the subject''s graph nodes and edges, and nulls the payload copy on closed cases. The row is kept as the record of the erasure with its counts, and holds no personal data other than the key value itself.';
                        COMMENT ON COLUMN public."ErasureRequest"."TenantRegistryId" IS 'The tenant the subject belongs to.';
                        COMMENT ON COLUMN public."ErasureRequest"."KeyName" IS 'The archive key name the subject is identified by, for example AccountId.';
                        COMMENT ON COLUMN public."ErasureRequest"."KeyValue" IS 'The key value of the subject, exactly as archived.';
                        COMMENT ON COLUMN public."ErasureRequest"."Reason" IS 'Why the erasure was requested, for example the data subject request reference.';
                        COMMENT ON COLUMN public."ErasureRequest"."StateId" IS 'Enumeration (ErasureState): 1 Pending approval, 2 Approved and waiting for the engine, 3 Executed, 4 Rejected, 5 Retracted, 6 Refused.';
                        COMMENT ON COLUMN public."ErasureRequest"."RefusalReasonId" IS 'Enumeration (ErasureRefusalReason): 1 Legal hold, 2 Open case, 3 Inside the retention minimum, 4 Nothing found. Set when the state is Refused.';
                        COMMENT ON COLUMN public."ErasureRequest"."ArchiveRowsFound" IS 'How many archived transactions matched the subject when the request was made or executed.';
                        COMMENT ON COLUMN public."ErasureRequest"."ArchiveRowsRemoved" IS 'How many archived transactions were removed.';
                        COMMENT ON COLUMN public."ErasureRequest"."GraphNodesRemoved" IS 'How many graph nodes for the subject were removed.';
                        COMMENT ON COLUMN public."ErasureRequest"."GraphEdgesRemoved" IS 'How many graph edges touching those nodes were removed.';
                        COMMENT ON COLUMN public."ErasureRequest"."CasePayloadsNulled" IS 'How many closed cases had their copy of the payload cleared. The cases themselves are kept.';
                        COMMENT ON COLUMN public."ErasureRequest"."RequestedUser" IS 'Who asked. Never the approver.';
                        COMMENT ON COLUMN public."ErasureRequest"."RequestedDate" IS 'UTC. When it was asked.';
                        COMMENT ON COLUMN public."ErasureRequest"."DecidedUser" IS 'Who approved, rejected or retracted it.';
                        COMMENT ON COLUMN public."ErasureRequest"."DecidedDate" IS 'UTC. When it was decided.';
                        COMMENT ON COLUMN public."ErasureRequest"."DecisionNote" IS 'The reason given with the decision.';
                        COMMENT ON COLUMN public."ErasureRequest"."ExecutedDate" IS 'UTC. When the engine executed or refused it.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

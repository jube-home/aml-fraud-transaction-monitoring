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

namespace Jube.Migrations.Branches.InboundQueue
{
    using FluentMigrator;

    [Migration(2026111105000000)]
    public class CreateTransactionInboundQueue : Migration
    {
        public override void Up()
        {
            Create.Table("TransactionInboundQueue")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().NotNullable()
                .WithColumn("Payload").AsBinary(int.MaxValue).NotNullable()
                .WithColumn("StatusId").AsInt16().NotNullable()
                .WithColumn("Attempts").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("AvailableDate").AsDateTime().NotNullable()
                .WithColumn("LockedUntil").AsDateTime().Nullable()
                .WithColumn("LockedBy").AsString(100).Nullable()
                .WithColumn("LastError").AsString(500).Nullable()
                .WithColumn("EnqueuedBy").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Index("IX_TransactionInboundQueue_Claim").OnTable("TransactionInboundQueue")
                .OnColumn("StatusId").Ascending().OnColumn("AvailableDate").Ascending().OnColumn("Id").Ascending();

            Execute.Sql("""
                        COMMENT ON TABLE public."TransactionInboundQueue" IS 'The default way to hand transactions to the engine without waiting for the answer: a durable queue in the same database, so no extra infrastructure is needed. A producer inserts a row (through the API or directly); an engine node claims a batch with FOR UPDATE SKIP LOCKED, runs each payload through the model, and deletes the row on success. A node that dies mid-batch simply stops renewing the claim and the rows become available again, so delivery is at least once.';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."EntityAnalysisModelGuid" IS 'The model that should process the payload. Joins EntityAnalysisModel on Guid.';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."Payload" IS 'The transaction exactly as the producer sent it, normally JSON bytes.';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."StatusId" IS 'Enumeration (TransactionInboundQueueStatus): 1 Pending, 2 Processing (claimed by a node), 3 Dead (gave up; kept for an operator to requeue or delete).';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."Attempts" IS 'How many times a node has claimed the row.';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."AvailableDate" IS 'UTC. Not claimed before this instant; pushed forward with a growing delay after each failure.';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."LockedUntil" IS 'UTC. While a node holds the row, until when; after this another node may take it.';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."LockedBy" IS 'The node that claimed the row.';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."LastError" IS 'A short plain reason for the last failure or for dead-lettering; never database text.';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."EnqueuedBy" IS 'The user whose API key enqueued the row.';
                        COMMENT ON COLUMN public."TransactionInboundQueue"."CreatedDate" IS 'UTC. When the row was enqueued.';
                        """);
        }

        public override void Down()
        {
        }
    }
}

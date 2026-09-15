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

namespace Jube.Migrations.Branches
{
    [Migration(20251113124600)]
    public class GitHubIssueBranch89 : Migration
    {
        public override void Up()
        {
            AddNewColumnsToArchive();
            AddNewColumnsToArchiveKey();
            AddNewColumnToEntityAnalysisAsynchronousQueueBalance();

            CreateCacheTtlCounterEntryRemovalBatch();
            CreateCacheTtlCounterEntryRemovalBatchEntry();
            CreateCacheTtlCounterEntryRemovalBatchResponseTime();
            CreateCachePayloadRemovalBatch();
            CreateCachePayloadRemovalBatchEntry();
            CreateCachePayloadRemovalBatchResponseTime();
            CreateCachePayloadLatestRemovalBatch();
            CreateCachePayloadLatestRemovalBatchEntry();
            CreateCachePayloadLatestRemovalBatchResponseTime();
            CreateArchiveVersion();
            CreateArchiveKeyVersion();
        }

        private void AddNewColumnToEntityAnalysisAsynchronousQueueBalance()
        {
            Alter.Table("EntityAnalysisAsynchronousQueueBalance").AddColumn("CaseCreation").AsInt32().Nullable();
            Alter.Table("EntityAnalysisAsynchronousQueueBalance").AddColumn("Tagging").AsInt32().Nullable();
            Alter.Table("EntityAnalysisAsynchronousQueueBalance").AddColumn("Notification").AsInt32().Nullable();
        }

        private void CreateArchiveKeyVersion()
        {
            Create.Table("ArchiveKeyVersion")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("ArchiveKeyId").AsInt64().Nullable()
                .WithColumn("ProcessingTypeId").AsByte().Nullable()
                .WithColumn("Key").AsString().Nullable()
                .WithColumn("KeyValueString").AsString().Nullable()
                .WithColumn("KeyValueInteger").AsInt32().Nullable()
                .WithColumn("KeyValueFloat").AsDouble().Nullable()
                .WithColumn("KeyValueBoolean").AsByte().Nullable()
                .WithColumn("KeyValueDate").AsDateTime2().Nullable()
                .WithColumn("KeyValueLong").AsInt64().Nullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelsReprocessingRuleInstanceId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ArchiveKeyVersion")
                .ForeignColumn("ArchiveKeyId")
                .ToTable("ArchiveKey").PrimaryColumn("Id");

            Create.Index().OnTable("ArchiveKeyVersion")
                .OnColumn("EntityAnalysisModelInstanceEntryGuid").Ascending()
                .OnColumn("Key").Ascending()
                .OnColumn("ProcessingTypeId");
        }

        private void AddNewColumnsToArchiveKey()
        {
            Alter.Table("ArchiveKey").AddColumn("KeyValueLong").AsInt64().Nullable();
            Alter.Table("ArchiveKey").AddColumn("Version").AsInt32().Nullable();
            Alter.Table("ArchiveKey").AddColumn("EntityAnalysisModelsReprocessingRuleInstanceId").AsInt32().Nullable();
        }

        private void CreateArchiveVersion()
        {
            Create.Table("ArchiveVersion")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("ArchiveId").AsInt64().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().Nullable()
                .WithColumn("EntryKeyValue").AsString().Nullable()
                .WithColumn("ResponseElevation").AsDouble().Nullable()
                .WithColumn("EntityAnalysisModelActivationRuleId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("ActivationRuleCount").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("ReferenceDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelsReprocessingRuleInstanceId").AsInt32().Nullable();

            Create.Index().OnTable("ArchiveVersion").OnColumn("ArchiveId").Ascending();

            Create.ForeignKey().FromTable("ArchiveVersion")
                .ForeignColumn("ArchiveId")
                .ToTable("Archive").PrimaryColumn("Id");
        }

        private void AddNewColumnsToArchive()
        {
            Alter.Table("Archive").AddColumn("Version").AsInt32().Nullable();
            Alter.Table("Archive").AddColumn("EntityAnalysisModelsReprocessingRuleInstanceId").AsInt32().Nullable();
        }

        private void CreateCachePayloadLatestRemovalBatchResponseTime()
        {
            Create.Table("CachePayloadLatestRemovalBatchResponseTime")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CachePayloadLatestRemovalBatchId").AsInt64()
                .WithColumn("TaskTypeId").AsInt32().Nullable()
                .WithColumn("ResponseTime").AsInt64().Nullable();

            Create.Index().OnTable("CachePayloadLatestRemovalBatchResponseTime")
                .OnColumn("CachePayloadLatestRemovalBatchId").Ascending();

            Create.ForeignKey().FromTable("CachePayloadLatestRemovalBatchResponseTime")
                .ForeignColumn("CachePayloadLatestRemovalBatchId")
                .ToTable("CachePayloadLatestRemovalBatch").PrimaryColumn("Id");
        }

        private void CreateCachePayloadLatestRemovalBatchEntry()
        {
            Create.Table("CachePayloadLatestRemovalBatchEntry")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CachePayloadLatestRemovalBatchId").AsInt64()
                .WithColumn("Value").AsString().Nullable()
                .WithColumn("ReferenceDate").AsDateTime().Nullable();

            Create.Index().OnTable("CachePayloadLatestRemovalBatchEntry")
                .OnColumn("CachePayloadLatestRemovalBatchId").Ascending();

            Create.ForeignKey().FromTable("CachePayloadLatestRemovalBatchEntry")
                .ForeignColumn("CachePayloadLatestRemovalBatchId")
                .ToTable("CachePayloadLatestRemovalBatch").PrimaryColumn("Id");
        }

        private void CreateCachePayloadLatestRemovalBatch()
        {
            Create.Table("CachePayloadLatestRemovalBatch")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelGuid").AsGuid()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("ReferenceDate").AsDateTime().Nullable()
                .WithColumn("Key").AsString().Nullable()
                .WithColumn("ExpiredSortedSetCount").AsInt32().NotNullable()
                .WithColumn("FirstExpiredSortedSetReferenceDate").AsDateTime().Nullable()
                .WithColumn("LastExpiredSortedSetReferenceDate").AsDateTime().Nullable()
                .WithColumn("FinishedDate").AsDateTime().Nullable();

            Create.Index().OnTable("CachePayloadLatestRemovalBatch")
                .OnColumn("EntityAnalysisModelGuid").Ascending();
        }

        private void CreateCachePayloadRemovalBatchResponseTime()
        {
            Create.Table("CachePayloadRemovalBatchResponseTime")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CachePayloadRemovalBatchId").AsInt64()
                .WithColumn("TaskTypeId").AsInt32().Nullable()
                .WithColumn("ResponseTime").AsInt64().Nullable();

            Create.Index().OnTable("CachePayloadRemovalBatchResponseTime")
                .OnColumn("CachePayloadRemovalBatchId").Ascending();

            Create.ForeignKey().FromTable("CachePayloadRemovalBatchResponseTime")
                .ForeignColumn("CachePayloadRemovalBatchId")
                .ToTable("CachePayloadRemovalBatch").PrimaryColumn("Id");
        }

        private void CreateCachePayloadRemovalBatchEntry()
        {
            Create.Table("CachePayloadRemovalBatchEntry")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CachePayloadRemovalBatchId").AsInt64()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("ReferenceDate").AsDateTime().Nullable();

            Create.Index().OnTable("CachePayloadRemovalBatchEntry")
                .OnColumn("CachePayloadRemovalBatchId").Ascending();

            Create.ForeignKey().FromTable("CachePayloadRemovalBatchEntry").ForeignColumn("CachePayloadRemovalBatchId")
                .ToTable("CachePayloadRemovalBatch").PrimaryColumn("Id");
        }

        private void CreateCachePayloadRemovalBatch()
        {
            Create.Table("CachePayloadRemovalBatch")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelGuid").AsGuid()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("ReferenceDate").AsDateTime().Nullable()
                .WithColumn("ExpiredSortedSetCount").AsInt32().NotNullable()
                .WithColumn("FirstExpiredSortedSetReferenceDate").AsDateTime().Nullable()
                .WithColumn("LastExpiredSortedSetReferenceDate").AsDateTime().Nullable()
                .WithColumn("FinishedDate").AsDateTime().Nullable();

            Create.Index().OnTable("CachePayloadRemovalBatch")
                .OnColumn("EntityAnalysisModelGuid").Ascending();
        }

        private void CreateCacheTtlCounterEntryRemovalBatchResponseTime()
        {
            Create.Table("CacheTtlCounterEntryRemovalBatchResponseTime")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CacheTtlCounterEntryRemovalBatchId").AsInt64()
                .WithColumn("TaskTypeId").AsInt32().Nullable()
                .WithColumn("ResponseTime").AsInt64().Nullable();

            Create.Index().OnTable("CacheTtlCounterEntryRemovalBatchResponseTime")
                .OnColumn("CacheTtlCounterEntryRemovalBatchId").Ascending();

            Create.ForeignKey().FromTable("CacheTtlCounterEntryRemovalBatchResponseTime")
                .ForeignColumn("CacheTtlCounterEntryRemovalBatchId")
                .ToTable("CacheTtlCounterEntryRemovalBatch").PrimaryColumn("Id");
        }

        private void CreateCacheTtlCounterEntryRemovalBatchEntry()
        {
            Create.Table("CacheTtlCounterEntryRemovalBatchEntry")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CacheTtlCounterEntryRemovalBatchId").AsInt64()
                .WithColumn("Value").AsString().Nullable()
                .WithColumn("ReferenceDate").AsDateTime().Nullable()
                .WithColumn("DecrementCount").AsInt32().Nullable()
                .WithColumn("RevisedCount").AsInt64().Nullable();

            Create.Index().OnTable("CacheTtlCounterEntryRemovalBatchEntry")
                .OnColumn("CacheTtlCounterEntryRemovalBatchId").Ascending();

            Create.ForeignKey().FromTable("CacheTtlCounterEntryRemovalBatchEntry")
                .ForeignColumn("CacheTtlCounterEntryRemovalBatchId")
                .ToTable("CacheTtlCounterEntryRemovalBatch").PrimaryColumn("Id");
        }

        private void CreateCacheTtlCounterEntryRemovalBatch()
        {
            Create.Table("CacheTtlCounterEntryRemovalBatch")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelTtlCounterGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("ReferenceDate").AsDateTime().Nullable()
                .WithColumn("ExpiredHashSetCount").AsInt32().NotNullable()
                .WithColumn("FirstExpiredHashSetReferenceDate").AsDateTime().Nullable()
                .WithColumn("LastExpiredHashSetReferenceDate").AsDateTime().Nullable()
                .WithColumn("FinishedDate").AsDateTime().Nullable();

            Create.Index().OnTable("CacheTtlCounterEntryRemovalBatch")
                .OnColumn("EntityAnalysisModelTtlCounterGuid").Ascending();
        }

        public override void Down()
        {
        }
    }
}
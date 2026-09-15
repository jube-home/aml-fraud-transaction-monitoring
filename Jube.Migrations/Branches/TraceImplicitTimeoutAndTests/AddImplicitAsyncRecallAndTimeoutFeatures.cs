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

using System;
using FluentMigrator;

namespace Jube.Migrations.Branches.TraceImplicitTimeoutAndTests
{
    [Migration(20260910130000)]
    public class AddImplicitAsyncRecallAndTimeoutFeatures : Migration
    {
        public override void Up()
        {
            Execute.Sql("""
                        DO $$
                        DECLARE
                            missing_extension text;
                        BEGIN
                            SELECT required.name INTO missing_extension
                            FROM (VALUES ('vector'), ('pg_trgm'), ('pg_stat_statements')) AS required(name)
                            WHERE NOT EXISTS (
                                SELECT 1 FROM pg_available_extensions
                                WHERE pg_available_extensions.name = required.name
                            )
                            LIMIT 1;

                            IF missing_extension IS NOT NULL THEN
                                RAISE EXCEPTION 'Extension "%" is not available on this server '
                                    '(no entry in pg_available_extensions) -- this Postgres node is still '
                                    'running an image that predates pgvector/postgresql-contrib. Deploy the '
                                    'updated image to every cluster member before re-running this migration.',
                                    missing_extension;
                            END IF;

                            IF current_setting('shared_preload_libraries') NOT LIKE '%pg_stat_statements%' THEN
                                RAISE EXCEPTION 'pg_stat_statements is not in shared_preload_libraries on this '
                                    'server. Run patronictl edit-config and a rolling restart first -- see '
                                    'Jube.Cluster/PgStatStatementsRollout.md -- then re-run this migration.';
                            END IF;
                        END $$;
                        """);

            Execute.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            Execute.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
            Execute.Sql("CREATE EXTENSION IF NOT EXISTS pg_stat_statements;");

            AddEntityAnalysisModelImplicitAsyncColumns();
            AddEntityAnalysisModelTraceColumn();
            AddEntityAnalysisModelInvocationTraceLogColumns();
            AddEntityAnalysisModelSamplingColumns();
            SplitEntityAnalysisModelLogsIntoInfoAndWarnThreshold();
            AddEntityAnalysisModelEnableLogsColumn();
            AddEntityAnalysisModelProcessingCounterImplicitAsyncColumns();
            AddEntityAnalysisModelProcessingCounterResponseTimeMinMaxColumns();
            AddEntityAnalysisModelProcessingCounterArchiveWalPendingCountColumn();
            AddAsynchronousQueueBalanceImplicitAsyncColumns();
            AddHttpProcessingCounterCallbackTimeoutColumn();
            AddLegacyProcessingCounterIndexes();
            CreateEntityAnalysisModelStagePerformanceCounter();
            CreateEntityAnalysisModelResponseTimePipelineCounter();
            CreateEntityAnalysisModelTaskPerformanceCounter();
            CreateDotNetRuntimeMetric();
            CreatePostgresMetric();
            CreateRedisMetric();
            CreateRedisSlowOperation();
            CreateApplicationLogEntry();
            CreateRedisConnectionMultiplexerMetric();
            CreateRedisConnectionEvent();
            CreateRedisSentinelStatus();
            CreateRedisSentinelEvent();
            CreateRedisCallCounter();
            CreatePostgresReplicationStatus();
            CreatePostgresLogEntry();
            CreateEtcdMemberStatus();
            CreatePatroniMemberStatus();
            CreateEtcdClusterEvent();
            CreatePatroniClusterEvent();
            CreateDockerContainerMetric();
            CreateDockerHostMetric();
            CreateDockerEvent();
            CreateContainerLogEntry();
            CreateOpenTelemetryMetric();
            AddUserLoginFailureMessageColumn();
            AddUserLoginSearchIndexes();
            CreateOpenTelemetryLogCounter();
            CreateOpenTelemetryExclude();
            GrantOpenTelemetryLogCounterAndExcludePermissions();
            CreateOtlpDispatchCounter();
            CreateModelInvokeWarning();
            CreateArchiverStagePerformanceCounter();
            CreateCaseCreationStagePerformanceCounter();
            CreateCaseCreationWarning();
            CreateArchiverWarning();
            CreateCaptureQueueHealth();
            CreateHaProxyServerStatus();
            CreateHaProxyReachabilityProbe();
            CreateOverlayNetworkTaskDrift();
            TrigramIndex("TenantRegistry", "Name");
            AddDictionaryEvalExpression();
        }

        private void AddDictionaryEvalExpression()
        {
            Create.Table("DictionaryEvalExpression")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Expression").AsString().Nullable()
                .WithColumn("ResultTypeId").AsInt32().Nullable()
                .WithColumn("Compiled").AsByte().Nullable()
                .WithColumn("CompileError").AsString().Nullable();
            
            Insert.IntoTable("DictionaryEvalExpression").Row(new
            {
                Name = "ShoutString",
                Expression = "value.ToUpper()",
                ResultTypeId = 1
            });
            
            Insert.IntoTable("DictionaryEvalExpression").Row(new
            {
                Name = "WhatIsLength",
                Expression = "value.Length",
                ResultTypeId = 2
            });
            
            Insert.IntoTable("DictionaryEvalExpression").Row(new
            {
                Name = "HalfLength",
                Expression = "value.Length / 2.0",
                ResultTypeId = 3
            });
            
            Insert.IntoTable("DictionaryEvalExpression").Row(new
            {
                Name = "ParseStringAsDate",
                Expression = "DateTime.Parse(value)",
                ResultTypeId = 4
            });
            
            Insert.IntoTable("DictionaryEvalExpression").Row(new
            {
                Name = "IsGmailEmail",
                Expression = "!value.EndsWith(\"gmail.com\")",
                ResultTypeId = 5
            });
        }

        private void AddEntityAnalysisModelImplicitAsyncColumns()
        {
            Alter.Table("EntityAnalysisModel").AddColumn("EnableImplicitAsync").AsByte().Nullable();
            Alter.Table("EntityAnalysisModel").AddColumn("ImplicitAsyncTimeoutMilliseconds").AsInt32().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("EnableImplicitAsync").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("ImplicitAsyncTimeoutMilliseconds").AsInt32()
                .Nullable();

            Update.Table("EntityAnalysisModel").Set(new { EnableImplicitAsync = 0 }).AllRows();
            Update.Table("EntityAnalysisModelVersion").Set(new { EnableImplicitAsync = 0 }).AllRows();
        }

        private void AddEntityAnalysisModelTraceColumn()
        {
            Alter.Table("EntityAnalysisModel").AddColumn("EnableTrace").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("EnableTrace").AsByte().Nullable();
            Update.Table("EntityAnalysisModel").Set(new { EnableTrace = 0 }).AllRows();
            Update.Table("EntityAnalysisModelVersion").Set(new { EnableTrace = 0 }).AllRows();
        }

        private void AddEntityAnalysisModelInvocationTraceLogColumns()
        {
            Alter.Table("EntityAnalysisModel").AddColumn("Logs").AsByte().Nullable();
            Alter.Table("EntityAnalysisModel").AddColumn("LogsWarnThresholdMilliseconds").AsInt32().Nullable();
            Alter.Table("EntityAnalysisModel").AddColumn("EnableLogsInResponse").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("Logs").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("LogsWarnThresholdMilliseconds").AsInt32().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("EnableLogsInResponse").AsByte().Nullable();

            Update.Table("EntityAnalysisModel").Set(new { Logs = 0, EnableLogsInResponse = 0 }).AllRows();
            Update.Table("EntityAnalysisModelVersion").Set(new { Logs = 0, EnableLogsInResponse = 0 }).AllRows();
        }

        private void AddEntityAnalysisModelSamplingColumns()
        {
            Alter.Table("EntityAnalysisModel").AddColumn("EnableSampling").AsByte().Nullable();
            Alter.Table("EntityAnalysisModel").AddColumn("SamplePercentage").AsDouble().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("EnableSampling").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("SamplePercentage").AsDouble().Nullable();

            Update.Table("EntityAnalysisModel").Set(new { EnableSampling = 0 }).AllRows();
            Update.Table("EntityAnalysisModelVersion").Set(new { EnableSampling = 0 }).AllRows();
        }

        private void SplitEntityAnalysisModelLogsIntoInfoAndWarnThreshold()
        {
            Alter.Table("EntityAnalysisModel").AddColumn("EnableLogsInfo").AsByte().Nullable();
            Alter.Table("EntityAnalysisModel").AddColumn("EnableLogsWarnThreshold").AsByte().Nullable();
            Execute.Sql("""
                        UPDATE "EntityAnalysisModel"
                        SET "EnableLogsInfo" = CASE WHEN "Logs" = 1 THEN 1 ELSE 0 END,
                            "EnableLogsWarnThreshold" = CASE WHEN "Logs" = 2 THEN 1 ELSE 0 END
                        """);
            Delete.Column("Logs").FromTable("EntityAnalysisModel");

            Alter.Table("EntityAnalysisModelVersion").AddColumn("EnableLogsInfo").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("EnableLogsWarnThreshold").AsByte().Nullable();
            Execute.Sql("""
                        UPDATE "EntityAnalysisModelVersion"
                        SET "EnableLogsInfo" = CASE WHEN "Logs" = 1 THEN 1 ELSE 0 END,
                            "EnableLogsWarnThreshold" = CASE WHEN "Logs" = 2 THEN 1 ELSE 0 END
                        """);
            Delete.Column("Logs").FromTable("EntityAnalysisModelVersion");
        }

        private void AddEntityAnalysisModelEnableLogsColumn()
        {
            Alter.Table("EntityAnalysisModel").AddColumn("EnableLogs").AsByte().Nullable();
            Alter.Table("EntityAnalysisModelVersion").AddColumn("EnableLogs").AsByte().Nullable();

            Execute.Sql("""
                        UPDATE "EntityAnalysisModel"
                        SET "EnableLogs" = CASE
                            WHEN "EnableLogsInfo" = 1 OR "EnableLogsWarnThreshold" = 1 THEN 1
                            ELSE 0
                        END
                        """);
            Execute.Sql("""
                        UPDATE "EntityAnalysisModelVersion"
                        SET "EnableLogs" = CASE
                            WHEN "EnableLogsInfo" = 1 OR "EnableLogsWarnThreshold" = 1 THEN 1
                            ELSE 0
                        END
                        """);
        }

        private void AddEntityAnalysisModelProcessingCounterImplicitAsyncColumns()
        {
            Alter.Table("EntityAnalysisModelProcessingCounter").AddColumn("ImplicitAsyncInvoke").AsInt32()
                .Nullable();
            Alter.Table("EntityAnalysisModelProcessingCounter").AddColumn("ImplicitAsyncTimeout").AsInt32()
                .Nullable();
            Alter.Table("EntityAnalysisModelProcessingCounter").AddColumn("ImplicitAsyncCompletedAfterTimeout")
                .AsInt32().Nullable();
            Alter.Table("EntityAnalysisModelProcessingCounter").AddColumn("ImplicitAsyncFaultedAfterTimeout")
                .AsInt32().Nullable();
        }

        private void AddEntityAnalysisModelProcessingCounterResponseTimeMinMaxColumns()
        {
            Alter.Table("EntityAnalysisModelProcessingCounter").AddColumn("MinResponseTimeMicroseconds")
                .AsInt64().Nullable();
            Alter.Table("EntityAnalysisModelProcessingCounter").AddColumn("MaxResponseTimeMicroseconds")
                .AsInt64().Nullable();
        }

        private void AddEntityAnalysisModelProcessingCounterArchiveWalPendingCountColumn()
        {
            Alter.Table("EntityAnalysisModelProcessingCounter").AddColumn("ArchiveWalPendingCount").AsInt32()
                .Nullable();
        }

        private void AddAsynchronousQueueBalanceImplicitAsyncColumns()
        {
            Alter.Table("EntityAnalysisModelAsynchronousQueueBalance").AddColumn("ImplicitAsyncOverdue")
                .AsInt32().Nullable();
            Alter.Table("EntityAnalysisAsynchronousQueueBalance").AddColumn("AsynchronousImplicitAsyncOverdue")
                .AsInt32().Nullable();
        }

        private void AddHttpProcessingCounterCallbackTimeoutColumn()
        {
            Alter.Table("HttpProcessingCounter").AddColumn("CallbackTimeout").AsInt32().Nullable();
        }

        private void AddLegacyProcessingCounterIndexes()
        {
            Create.Index().OnTable("HttpProcessingCounter").OnColumn("CreatedDate").Ascending();
            Create.Index().OnTable("EntityAnalysisAsynchronousQueueBalance").OnColumn("CreatedDate").Ascending();
            Create.Index().OnTable("EntityAnalysisModelAsynchronousQueueBalance").OnColumn("CreatedDate")
                .Ascending();
            Create.Index().OnTable("EntityAnalysisModelProcessingCounter").OnColumn("CreatedDate").Ascending();
            TrigramIndex("EntityAnalysisModel", "Name");
        }

        private void CreateEntityAnalysisModelStagePerformanceCounter()
        {
            Create.Table("EntityAnalysisModelStagePerformanceCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("StageId").AsInt32().Nullable()
                .WithColumn("TotalMicroseconds").AsInt64().Nullable()
                .WithColumn("MinMicroseconds").AsInt64().Nullable()
                .WithColumn("MaxMicroseconds").AsInt64().Nullable()
                .WithColumn("InvokeCount").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("EntityAnalysisModelStagePerformanceCounter", "CreatedDate");
            Create.Index().OnTable("EntityAnalysisModelStagePerformanceCounter").OnColumn("StageId").Ascending();
            Create.Index().OnTable("EntityAnalysisModelStagePerformanceCounter").OnColumn("EntityAnalysisModelGuid")
                .Ascending();
        }

        private void CreateEntityAnalysisModelResponseTimePipelineCounter()
        {
            Create.Table("EntityAnalysisModelResponseTimePipelineCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("StageId").AsInt32().Nullable()
                .WithColumn("SequenceNumber").AsInt32().Nullable()
                .WithColumn("TotalMicroseconds").AsInt64().Nullable()
                .WithColumn("MinMicroseconds").AsInt64().Nullable()
                .WithColumn("MaxMicroseconds").AsInt64().Nullable()
                .WithColumn("TotalAllocatedBytes").AsInt64().Nullable()
                .WithColumn("MinAllocatedBytes").AsInt64().Nullable()
                .WithColumn("MaxAllocatedBytes").AsInt64().Nullable()
                .WithColumn("InvokeCount").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("EntityAnalysisModelResponseTimePipelineCounter", "CreatedDate");
            Create.Index().OnTable("EntityAnalysisModelResponseTimePipelineCounter").OnColumn("StageId")
                .Ascending();
            Create.Index().OnTable("EntityAnalysisModelResponseTimePipelineCounter")
                .OnColumn("EntityAnalysisModelGuid").Ascending();
        }

        private void CreateEntityAnalysisModelTaskPerformanceCounter()
        {
            Create.Table("EntityAnalysisModelTaskPerformanceCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("DirectionId").AsInt32().Nullable()
                .WithColumn("TaskTypeId").AsInt32().Nullable()
                .WithColumn("TotalMicroseconds").AsInt64().Nullable()
                .WithColumn("MinMicroseconds").AsInt64().Nullable()
                .WithColumn("MaxMicroseconds").AsInt64().Nullable()
                .WithColumn("TotalAllocatedBytes").AsInt64().Nullable()
                .WithColumn("MinAllocatedBytes").AsInt64().Nullable()
                .WithColumn("MaxAllocatedBytes").AsInt64().Nullable()
                .WithColumn("InvokeCount").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("EntityAnalysisModelTaskPerformanceCounter", "CreatedDate");
            Create.Index().OnTable("EntityAnalysisModelTaskPerformanceCounter").OnColumn("DirectionId").Ascending();
            Create.Index().OnTable("EntityAnalysisModelTaskPerformanceCounter").OnColumn("TaskTypeId").Ascending();
            Create.Index().OnTable("EntityAnalysisModelTaskPerformanceCounter").OnColumn("EntityAnalysisModelGuid")
                .Ascending();
        }

        private void CreateDotNetRuntimeMetric()
        {
            Create.Table("DotNetRuntimeMetric")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable()
                .WithColumn("ProcessorCount").AsInt32().Nullable()
                .WithColumn("Gen0CollectionCount").AsInt32().Nullable()
                .WithColumn("Gen1CollectionCount").AsInt32().Nullable()
                .WithColumn("Gen2CollectionCount").AsInt32().Nullable()
                .WithColumn("TotalAllocatedBytes").AsInt64().Nullable()
                .WithColumn("HeapSizeBytes").AsInt64().Nullable()
                .WithColumn("FragmentedBytes").AsInt64().Nullable()
                .WithColumn("MemoryLoadBytes").AsInt64().Nullable()
                .WithColumn("HighMemoryLoadThresholdBytes").AsInt64().Nullable()
                .WithColumn("WorkingSetBytes").AsInt64().Nullable()
                .WithColumn("PrivateMemoryBytes").AsInt64().Nullable()
                .WithColumn("ThreadCount").AsInt32().Nullable()
                .WithColumn("ThreadPoolWorkerThreadsAvailable").AsInt32().Nullable()
                .WithColumn("ThreadPoolWorkerThreadsMax").AsInt32().Nullable()
                .WithColumn("ThreadPoolCompletionPortThreadsAvailable").AsInt32().Nullable()
                .WithColumn("ThreadPoolCompletionPortThreadsMax").AsInt32().Nullable()
                .WithColumn("ThreadPoolQueueLength").AsInt64().Nullable()
                .WithColumn("CpuTimeMicroseconds").AsInt64().Nullable();

            Alter.Table("DotNetRuntimeMetric").AddColumn("RuntimeAvailableMemoryBytes").AsInt64().Nullable();
            Alter.Table("DotNetRuntimeMetric").AddColumn("RuntimeCommittedMemoryBytes").AsInt64().Nullable();
            Alter.Table("DotNetRuntimeMetric").AddColumn("ContainerCpuLimitCores").AsDouble().Nullable();
            Alter.Table("DotNetRuntimeMetric").AddColumn("ContainerCpuUsageMicroseconds").AsInt64().Nullable();
            Alter.Table("DotNetRuntimeMetric").AddColumn("ContainerCpuThrottledPeriods").AsInt64().Nullable();
            Alter.Table("DotNetRuntimeMetric").AddColumn("ContainerCpuThrottledMicroseconds").AsInt64().Nullable();
            Alter.Table("DotNetRuntimeMetric").AddColumn("ContainerMemoryLimitBytes").AsInt64().Nullable();
            Alter.Table("DotNetRuntimeMetric").AddColumn("ContainerMemoryUsageBytes").AsInt64().Nullable();
            Alter.Table("DotNetRuntimeMetric").AddColumn("GcPauseTimeMicroseconds").AsInt64().Nullable();
            Alter.Table("DotNetRuntimeMetric").AddColumn("LockContentionCount").AsInt64().Nullable();

            IndexDateColumn("DotNetRuntimeMetric", "CreatedDate");
            TrigramIndex("DotNetRuntimeMetric", "Instance");
        }

        private void CreatePostgresMetric()
        {
            Create.Table("PostgresMetric")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable()
                .WithColumn("ActiveConnections").AsInt32().Nullable()
                .WithColumn("TransactionsCommitted").AsInt64().Nullable()
                .WithColumn("TransactionsRolledBack").AsInt64().Nullable()
                .WithColumn("BlocksRead").AsInt64().Nullable()
                .WithColumn("BlocksHit").AsInt64().Nullable()
                .WithColumn("CacheHitRatioPercent").AsDouble().Nullable()
                .WithColumn("RowsReturned").AsInt64().Nullable()
                .WithColumn("RowsFetched").AsInt64().Nullable()
                .WithColumn("RowsInserted").AsInt64().Nullable()
                .WithColumn("RowsUpdated").AsInt64().Nullable()
                .WithColumn("RowsDeleted").AsInt64().Nullable()
                .WithColumn("Deadlocks").AsInt64().Nullable()
                .WithColumn("TempFilesCreated").AsInt64().Nullable()
                .WithColumn("TempBytesWritten").AsInt64().Nullable()
                .WithColumn("Conflicts").AsInt64().Nullable()
                .WithColumn("IsInRecovery").AsBoolean().Nullable()
                .WithColumn("ReplicationLagSeconds").AsDouble().Nullable()
                .WithColumn("ReplicaCount").AsInt32().Nullable();

            Alter.Table("PostgresMetric").AddColumn("DatabaseSizeBytes").AsInt64().Nullable();
            Alter.Table("PostgresMetric").AddColumn("LongestRunningQuerySeconds").AsDouble().Nullable();
            Alter.Table("PostgresMetric").AddColumn("WaitingBackends").AsInt32().Nullable();
            Alter.Table("PostgresMetric").AddColumn("WalBytesGenerated").AsInt64().Nullable();

            IndexDateColumn("PostgresMetric", "CreatedDate");
            TrigramIndex("PostgresMetric", "Instance");
        }

        private void CreateRedisMetric()
        {
            Create.Table("RedisMetric")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable()
                .WithColumn("ConnectedClients").AsInt32().Nullable()
                .WithColumn("BlockedClients").AsInt32().Nullable()
                .WithColumn("UsedMemoryBytes").AsInt64().Nullable()
                .WithColumn("UsedMemoryRssBytes").AsInt64().Nullable()
                .WithColumn("MaxMemoryBytes").AsInt64().Nullable()
                .WithColumn("InstantaneousOpsPerSecond").AsInt32().Nullable()
                .WithColumn("TotalCommandsProcessed").AsInt64().Nullable()
                .WithColumn("TotalConnectionsReceived").AsInt64().Nullable()
                .WithColumn("KeyspaceHits").AsInt64().Nullable()
                .WithColumn("KeyspaceMisses").AsInt64().Nullable()
                .WithColumn("HitRatePercent").AsDouble().Nullable()
                .WithColumn("EvictedKeys").AsInt64().Nullable()
                .WithColumn("ExpiredKeys").AsInt64().Nullable()
                .WithColumn("ConnectedReplicas").AsInt32().Nullable()
                .WithColumn("MasterReplicationOffset").AsInt64().Nullable()
                .WithColumn("UptimeSeconds").AsInt64().Nullable();

            Alter.Table("RedisMetric").AddColumn("TotalKeys").AsInt64().Nullable();
            Alter.Table("RedisMetric").AddColumn("MemoryFragmentationRatio").AsDouble().Nullable();
            Alter.Table("RedisMetric").AddColumn("RdbLastSaveAgeSeconds").AsInt64().Nullable();
            Alter.Table("RedisMetric").AddColumn("AofEnabled").AsBoolean().Nullable();
            Alter.Table("RedisMetric").AddColumn("LastAofRewriteDate").AsDateTime().Nullable();
            Alter.Table("RedisMetric").AddColumn("LastBgSaveDate").AsDateTime().Nullable();

            IndexDateColumn("RedisMetric", "CreatedDate");
            TrigramIndex("RedisMetric", "Instance");
        }

        private void CreateRedisSlowOperation()
        {
            Create.Table("RedisSlowOperation")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("RedisSlowLogId").AsInt64().Nullable()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("DurationMicroseconds").AsInt64().Nullable()
                .WithColumn("Command").AsString(1024).Nullable()
                .WithColumn("ClientAddress").AsString(255).Nullable()
                .WithColumn("ClientName").AsString(255).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            Alter.Table("RedisSlowOperation").AddColumn("CommandName").AsString(64).Nullable();
            Alter.Table("RedisSlowOperation").AddColumn("KeyName").AsString(1024).Nullable();

            IndexDateColumn("RedisSlowOperation", "OccurredDate");
            TrigramIndex("RedisSlowOperation", "Command");
            TrigramIndex("RedisSlowOperation", "CommandName");
            TrigramIndex("RedisSlowOperation", "KeyName");
            TrigramIndex("RedisSlowOperation", "ClientAddress");
            TrigramIndex("RedisSlowOperation", "ClientName");
        }

        private void CreateApplicationLogEntry()
        {
            Create.Table("ApplicationLogEntry")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("Level").AsString(16).Nullable()
                .WithColumn("LoggerName").AsString(1024).Nullable()
                .WithColumn("ThreadContext").AsString(255).Nullable()
                .WithColumn("Message").AsString(4096).Nullable()
                .WithColumn("Exception").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("ApplicationLogEntry", "OccurredDate");
            TrigramIndex("ApplicationLogEntry", "Message");
            TrigramIndex("ApplicationLogEntry", "LoggerName");
            TrigramIndex("ApplicationLogEntry", "Exception");
        }

        private void CreateRedisConnectionMultiplexerMetric()
        {
            Create.Table("RedisConnectionMultiplexerMetric")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable()
                .WithColumn("ClientName").AsString(255).Nullable()
                .WithColumn("TimeoutMilliseconds").AsInt32().Nullable()
                .WithColumn("IsConnected").AsBoolean().Nullable()
                .WithColumn("IsConnecting").AsBoolean().Nullable()
                .WithColumn("EndPointCount").AsInt32().Nullable()
                .WithColumn("ConnectedEndPointCount").AsInt32().Nullable()
                .WithColumn("TotalOperationCount").AsInt64().Nullable()
                .WithColumn("TotalOutstanding").AsInt32().Nullable()
                .WithColumn("InteractivePendingUnsentItems").AsInt32().Nullable()
                .WithColumn("InteractiveSentItemsAwaitingResponse").AsInt32().Nullable()
                .WithColumn("InteractiveResponsesAwaitingAsyncCompletion").AsInt32().Nullable()
                .WithColumn("InteractiveTotalOutstanding").AsInt32().Nullable()
                .WithColumn("InteractiveCompletedAsynchronously").AsInt64().Nullable()
                .WithColumn("InteractiveCompletedSynchronously").AsInt64().Nullable()
                .WithColumn("InteractiveFailedAsynchronously").AsInt64().Nullable()
                .WithColumn("InteractiveNonPreferredEndpointCount").AsInt64().Nullable()
                .WithColumn("InteractiveSocketCount").AsInt64().Nullable()
                .WithColumn("InteractiveWriterCount").AsInt32().Nullable()
                .WithColumn("SubscriptionPendingUnsentItems").AsInt32().Nullable()
                .WithColumn("SubscriptionSentItemsAwaitingResponse").AsInt32().Nullable()
                .WithColumn("SubscriptionResponsesAwaitingAsyncCompletion").AsInt32().Nullable()
                .WithColumn("SubscriptionTotalOutstanding").AsInt32().Nullable()
                .WithColumn("SubscriptionCompletedAsynchronously").AsInt64().Nullable()
                .WithColumn("SubscriptionCompletedSynchronously").AsInt64().Nullable()
                .WithColumn("SubscriptionFailedAsynchronously").AsInt64().Nullable()
                .WithColumn("SubscriptionSocketCount").AsInt64().Nullable()
                .WithColumn("SubscriptionCount").AsInt64().Nullable();

            Alter.Table("RedisConnectionMultiplexerMetric").AddColumn("ConnectionFailedCount").AsInt64()
                .Nullable();
            Alter.Table("RedisConnectionMultiplexerMetric").AddColumn("ConnectionRestoredCount").AsInt64()
                .Nullable();
            Alter.Table("RedisConnectionMultiplexerMetric").AddColumn("ErrorMessageCount").AsInt64().Nullable();
            Alter.Table("RedisConnectionMultiplexerMetric").AddColumn("InternalErrorCount").AsInt64().Nullable();
            Alter.Table("RedisConnectionMultiplexerMetric").AddColumn("ConfigurationChangedCount").AsInt64()
                .Nullable();
            Alter.Table("RedisConnectionMultiplexerMetric").AddColumn("ConfigurationChangedBroadcastCount")
                .AsInt64().Nullable();

            IndexDateColumn("RedisConnectionMultiplexerMetric", "CreatedDate");
            TrigramIndex("RedisConnectionMultiplexerMetric", "Instance");
            TrigramIndex("RedisConnectionMultiplexerMetric", "ClientName");
        }

        private void CreateRedisConnectionEvent()
        {
            Create.Table("RedisConnectionEvent")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("EventTypeId").AsInt32().Nullable()
                .WithColumn("EndPoint").AsString(255).Nullable()
                .WithColumn("ConnectionTypeId").AsInt32().Nullable()
                .WithColumn("FailureTypeId").AsInt32().Nullable()
                .WithColumn("Origin").AsString(255).Nullable()
                .WithColumn("Message").AsString(2048).Nullable()
                .WithColumn("Exception").AsString(4000).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            Alter.Table("RedisConnectionEvent").AddColumn("RetryCount").AsInt32().Nullable();
            Alter.Table("RedisConnectionEvent").AddColumn("BackoffMilliseconds").AsInt32().Nullable();
            Alter.Table("RedisConnectionEvent").AddColumn("TransactionsImpacted").AsInt32().Nullable();

            IndexDateColumn("RedisConnectionEvent", "OccurredDate");
            TrigramIndex("RedisConnectionEvent", "EndPoint");
            TrigramIndex("RedisConnectionEvent", "Message");
            TrigramIndex("RedisConnectionEvent", "Exception");
            Create.Index().OnTable("RedisConnectionEvent").OnColumn("EventTypeId").Ascending();
            Create.Index().OnTable("RedisConnectionEvent").OnColumn("ConnectionTypeId").Ascending();
            Create.Index().OnTable("RedisConnectionEvent").OnColumn("FailureTypeId").Ascending();
        }

        private void CreateRedisSentinelStatus()
        {
            Create.Table("RedisSentinelStatus")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("EntityTypeId").AsInt32().Nullable()
                .WithColumn("Name").AsString(255).Nullable()
                .WithColumn("Ip").AsString(64).Nullable()
                .WithColumn("Port").AsInt32().Nullable()
                .WithColumn("Flags").AsString(128).Nullable()
                .WithColumn("MasterLinkStatus").AsString(32).Nullable()
                .WithColumn("MasterHost").AsString(64).Nullable()
                .WithColumn("MasterPort").AsInt32().Nullable()
                .WithColumn("SlaveReplOffset").AsInt64().Nullable()
                .WithColumn("NumSlaves").AsInt32().Nullable()
                .WithColumn("NumOtherSentinels").AsInt32().Nullable()
                .WithColumn("Quorum").AsInt32().Nullable()
                .WithColumn("DownAfterMilliseconds").AsInt64().Nullable()
                .WithColumn("RunId").AsString(64).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            Alter.Table("RedisSentinelStatus").AddColumn("ReplicationLagSeconds").AsDouble().Nullable();

            IndexDateColumn("RedisSentinelStatus", "OccurredDate");
            TrigramIndex("RedisSentinelStatus", "Name");
            TrigramIndex("RedisSentinelStatus", "Ip");
            TrigramIndex("RedisSentinelStatus", "Flags");
            TrigramIndex("RedisSentinelStatus", "MasterLinkStatus");
            Create.Index().OnTable("RedisSentinelStatus").OnColumn("EntityTypeId").Ascending();
        }

        private void CreateRedisSentinelEvent()
        {
            Create.Table("RedisSentinelEvent")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("Channel").AsString(128).Nullable()
                .WithColumn("Message").AsString(2048).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("RedisSentinelEvent", "OccurredDate");
            TrigramIndex("RedisSentinelEvent", "Channel");
            TrigramIndex("RedisSentinelEvent", "Message");
        }

        private void CreateRedisCallCounter()
        {
            Create.Table("RedisCallCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Call").AsString(255).Nullable()
                .WithColumn("Count").AsInt64().Nullable()
                .WithColumn("TotalMicroseconds").AsInt64().Nullable()
                .WithColumn("MinMicroseconds").AsInt64().Nullable()
                .WithColumn("MaxMicroseconds").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("RedisCallCounter", "CreatedDate");
            TrigramIndex("RedisCallCounter", "Call");
        }

        private void CreatePostgresReplicationStatus()
        {
            Create.Table("PostgresReplicationStatus")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("Pid").AsInt32().Nullable()
                .WithColumn("UserName").AsString(255).Nullable()
                .WithColumn("ApplicationName").AsString(255).Nullable()
                .WithColumn("ClientAddress").AsString(64).Nullable()
                .WithColumn("State").AsString(32).Nullable()
                .WithColumn("SentLsn").AsString(32).Nullable()
                .WithColumn("WriteLsn").AsString(32).Nullable()
                .WithColumn("FlushLsn").AsString(32).Nullable()
                .WithColumn("ReplayLsn").AsString(32).Nullable()
                .WithColumn("WriteLagSeconds").AsDouble().Nullable()
                .WithColumn("FlushLagSeconds").AsDouble().Nullable()
                .WithColumn("ReplayLagSeconds").AsDouble().Nullable()
                .WithColumn("SyncState").AsString(32).Nullable()
                .WithColumn("SyncPriority").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("PostgresReplicationStatus", "OccurredDate");
            TrigramIndex("PostgresReplicationStatus", "UserName");
            TrigramIndex("PostgresReplicationStatus", "ApplicationName");
            TrigramIndex("PostgresReplicationStatus", "ClientAddress");
            TrigramIndex("PostgresReplicationStatus", "State");
            TrigramIndex("PostgresReplicationStatus", "SyncState");
        }

        private void CreatePostgresLogEntry()
        {
            Create.Table("PostgresLogEntry")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("Level").AsString(16).Nullable()
                .WithColumn("Pid").AsInt32().Nullable()
                .WithColumn("Message").AsString(4096).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("PostgresLogEntry", "OccurredDate");
            TrigramIndex("PostgresLogEntry", "Message");
            TrigramIndex("PostgresLogEntry", "Level");
        }

        private void CreateEtcdMemberStatus()
        {
            Create.Table("EtcdMemberStatus")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("Endpoint").AsString(255).Nullable()
                .WithColumn("MemberId").AsString(32).Nullable()
                .WithColumn("Name").AsString(255).Nullable()
                .WithColumn("PeerUrls").AsString(1024).Nullable()
                .WithColumn("ClientUrls").AsString(1024).Nullable()
                .WithColumn("IsLearner").AsBoolean().Nullable()
                .WithColumn("Version").AsString(32).Nullable()
                .WithColumn("ClusterVersion").AsString(32).Nullable()
                .WithColumn("HealthOk").AsBoolean().Nullable()
                .WithColumn("HealthReason").AsString(255).Nullable()
                .WithColumn("LeaderId").AsString(32).Nullable()
                .WithColumn("IsLeader").AsBoolean().Nullable()
                .WithColumn("HasLeader").AsBoolean().Nullable()
                .WithColumn("LeaderChangesTotal").AsInt64().Nullable()
                .WithColumn("DbSizeBytes").AsInt64().Nullable()
                .WithColumn("DbSizeInUseBytes").AsInt64().Nullable()
                .WithColumn("RaftIndex").AsInt64().Nullable()
                .WithColumn("RaftTerm").AsInt64().Nullable()
                .WithColumn("RaftAppliedIndex").AsInt64().Nullable()
                .WithColumn("ProposalsCommittedTotal").AsInt64().Nullable()
                .WithColumn("ProposalsAppliedTotal").AsInt64().Nullable()
                .WithColumn("ProposalsPendingCount").AsInt64().Nullable()
                .WithColumn("ProposalsFailedTotal").AsInt64().Nullable()
                .WithColumn("WalFsyncAvgMicroseconds").AsDouble().Nullable()
                .WithColumn("BackendCommitAvgMicroseconds").AsDouble().Nullable()
                .WithColumn("SlowApplyTotal").AsInt64().Nullable()
                .WithColumn("SlowReadIndexesTotal").AsInt64().Nullable()
                .WithColumn("AlarmCount").AsInt32().Nullable()
                .WithColumn("Alarms").AsString(255).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("EtcdMemberStatus", "OccurredDate");
            TrigramIndex("EtcdMemberStatus", "Endpoint");
            TrigramIndex("EtcdMemberStatus", "Name");
            TrigramIndex("EtcdMemberStatus", "HealthReason");
            TrigramIndex("EtcdMemberStatus", "Alarms");
        }

        private void CreatePatroniMemberStatus()
        {
            Create.Table("PatroniMemberStatus")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("Name").AsString(255).Nullable()
                .WithColumn("Host").AsString(255).Nullable()
                .WithColumn("Port").AsInt32().Nullable()
                .WithColumn("ApiUrl").AsString(255).Nullable()
                .WithColumn("Role").AsString(32).Nullable()
                .WithColumn("State").AsString(32).Nullable()
                .WithColumn("TimelineId").AsInt32().Nullable()
                .WithColumn("LagBytes").AsInt64().Nullable()
                .WithColumn("PendingRestart").AsBoolean().Nullable()
                .WithColumn("PatroniVersion").AsString(32).Nullable()
                .WithColumn("Scope").AsString(255).Nullable()
                .WithColumn("PostgresServerVersion").AsInt32().Nullable()
                .WithColumn("DatabaseSystemIdentifier").AsString(32).Nullable()
                .WithColumn("XlogLocationBytes").AsInt64().Nullable()
                .WithColumn("ReceivedLocationBytes").AsInt64().Nullable()
                .WithColumn("ReplayPaused").AsBoolean().Nullable()
                .WithColumn("ClusterUnlocked").AsBoolean().Nullable()
                .WithColumn("SyncStandby").AsBoolean().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("PatroniMemberStatus", "OccurredDate");
            TrigramIndex("PatroniMemberStatus", "Name");
            TrigramIndex("PatroniMemberStatus", "Host");
            TrigramIndex("PatroniMemberStatus", "Role");
            TrigramIndex("PatroniMemberStatus", "State");
            TrigramIndex("PatroniMemberStatus", "Scope");
        }

        private void CreateEtcdClusterEvent()
        {
            Create.Table("EtcdClusterEvent")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("Endpoint").AsString(255).Nullable()
                .WithColumn("MemberId").AsString(32).Nullable()
                .WithColumn("Name").AsString(255).Nullable()
                .WithColumn("EventTypeId").AsInt32().Nullable()
                .WithColumn("PreviousValue").AsString(255).Nullable()
                .WithColumn("NewValue").AsString(255).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("EtcdClusterEvent", "OccurredDate");
            TrigramIndex("EtcdClusterEvent", "Endpoint");
            TrigramIndex("EtcdClusterEvent", "Name");
            TrigramIndex("EtcdClusterEvent", "PreviousValue");
            TrigramIndex("EtcdClusterEvent", "NewValue");
            Create.Index().OnTable("EtcdClusterEvent").OnColumn("EventTypeId").Ascending();
        }

        private void CreatePatroniClusterEvent()
        {
            Create.Table("PatroniClusterEvent")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("Scope").AsString(255).Nullable()
                .WithColumn("Name").AsString(255).Nullable()
                .WithColumn("EventTypeId").AsInt32().Nullable()
                .WithColumn("PreviousValue").AsString(255).Nullable()
                .WithColumn("NewValue").AsString(255).Nullable()
                .WithColumn("Reason").AsString(255).Nullable()
                .WithColumn("TimelineId").AsInt32().Nullable()
                .WithColumn("LsnBytes").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("PatroniClusterEvent", "OccurredDate");
            TrigramIndex("PatroniClusterEvent", "Scope");
            TrigramIndex("PatroniClusterEvent", "Name");
            TrigramIndex("PatroniClusterEvent", "PreviousValue");
            TrigramIndex("PatroniClusterEvent", "NewValue");
            TrigramIndex("PatroniClusterEvent", "Reason");
            Create.Index().OnTable("PatroniClusterEvent").OnColumn("EventTypeId").Ascending();
        }

        private void CreateDockerContainerMetric()
        {
            Create.Table("DockerContainerMetric")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("ContainerId").AsString(64).Nullable()
                .WithColumn("Name").AsString(255).Nullable()
                .WithColumn("Image").AsString(255).Nullable()
                .WithColumn("State").AsString(32).Nullable()
                .WithColumn("Status").AsString(255).Nullable()
                .WithColumn("RestartCount").AsInt32().Nullable()
                .WithColumn("OomKilled").AsBoolean().Nullable()
                .WithColumn("ExitCode").AsInt32().Nullable()
                .WithColumn("StartedAt").AsDateTime().Nullable()
                .WithColumn("HealthStatus").AsString(32).Nullable()
                .WithColumn("CpuUsagePercent").AsDouble().Nullable()
                .WithColumn("OnlineCpus").AsInt32().Nullable()
                .WithColumn("MemoryUsageBytes").AsInt64().Nullable()
                .WithColumn("MemoryLimitBytes").AsInt64().Nullable()
                .WithColumn("MemoryPercent").AsDouble().Nullable()
                .WithColumn("NetworkRxBytes").AsInt64().Nullable()
                .WithColumn("NetworkTxBytes").AsInt64().Nullable()
                .WithColumn("BlockReadBytes").AsInt64().Nullable()
                .WithColumn("BlockWriteBytes").AsInt64().Nullable()
                .WithColumn("PidsCurrent").AsInt32().Nullable()
                .WithColumn("PidsLimit").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("DockerContainerMetric", "OccurredDate");
            TrigramIndex("DockerContainerMetric", "Name");
            TrigramIndex("DockerContainerMetric", "Image");
            TrigramIndex("DockerContainerMetric", "State");
            TrigramIndex("DockerContainerMetric", "Status");
        }

        private void CreateDockerHostMetric()
        {
            Create.Table("DockerHostMetric")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ContainersTotal").AsInt32().Nullable()
                .WithColumn("ContainersRunning").AsInt32().Nullable()
                .WithColumn("ContainersPaused").AsInt32().Nullable()
                .WithColumn("ContainersStopped").AsInt32().Nullable()
                .WithColumn("ImagesCount").AsInt32().Nullable()
                .WithColumn("NCpu").AsInt32().Nullable()
                .WithColumn("MemTotalBytes").AsInt64().Nullable()
                .WithColumn("DockerVersion").AsString(32).Nullable()
                .WithColumn("ApiVersion").AsString(32).Nullable()
                .WithColumn("KernelVersion").AsString(64).Nullable()
                .WithColumn("OperatingSystem").AsString(255).Nullable()
                .WithColumn("OsType").AsString(32).Nullable()
                .WithColumn("Architecture").AsString(32).Nullable()
                .WithColumn("LayersSizeBytes").AsInt64().Nullable()
                .WithColumn("ImagesSizeBytes").AsInt64().Nullable()
                .WithColumn("ReclaimableImagesBytes").AsInt64().Nullable()
                .WithColumn("ContainersDiskBytes").AsInt64().Nullable()
                .WithColumn("VolumesSizeBytes").AsInt64().Nullable()
                .WithColumn("BuildCacheSizeBytes").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("DockerHostMetric", "CreatedDate");
            TrigramIndex("DockerHostMetric", "Instance");
            TrigramIndex("DockerHostMetric", "OperatingSystem");
        }

        private void CreateDockerEvent()
        {
            Create.Table("DockerEvent")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("EventType").AsString(64).Nullable()
                .WithColumn("Action").AsString(4096).Nullable()
                .WithColumn("ActorId").AsString(255).Nullable()
                .WithColumn("ActorName").AsString(255).Nullable()
                .WithColumn("Scope").AsString(16).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("DockerEvent", "OccurredDate");
            TrigramIndex("DockerEvent", "Action");
            TrigramIndex("DockerEvent", "ActorName");
            TrigramIndex("DockerEvent", "EventType");
        }

        private void CreateContainerLogEntry()
        {
            Create.Table("ContainerLogEntry")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("ContainerName").AsString(255).Nullable()
                .WithColumn("StreamTypeId").AsInt32().Nullable()
                .WithColumn("Message").AsString(4096).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("ContainerLogEntry", "OccurredDate");
            TrigramIndex("ContainerLogEntry", "Message");
            TrigramIndex("ContainerLogEntry", "ContainerName");
            Create.Index().OnTable("ContainerLogEntry").OnColumn("StreamTypeId").Ascending();
        }

        private void CreateOpenTelemetryMetric()
        {
            Create.Table("OpenTelemetryMetric")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("MetricName").AsString(255).Nullable()
                .WithColumn("InstrumentType").AsString(32).Nullable()
                .WithColumn("Tags").AsString(1024).Nullable()
                .WithColumn("Count").AsInt64().Nullable()
                .WithColumn("Sum").AsDouble().Nullable()
                .WithColumn("Min").AsDouble().Nullable()
                .WithColumn("Max").AsDouble().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("OpenTelemetryMetric", "OccurredDate");
            TrigramIndex("OpenTelemetryMetric", "MetricName");
            TrigramIndex("OpenTelemetryMetric", "Tags");
        }

        private void AddUserLoginFailureMessageColumn()
        {
            Alter.Table("UserLogin").AddColumn("FailureMessage").AsString(2048).Nullable();
        }

        private void AddUserLoginSearchIndexes()
        {
            Create.Index("IX_UserLogin_CreatedDate").OnTable("UserLogin").OnColumn("CreatedDate").Ascending();

            TrigramIndex("UserLogin", "CreatedUser");
            TrigramIndex("UserLogin", "RemoteIp");
            TrigramIndex("UserLogin", "UserAgent");
            TrigramIndex("UserLogin", "FailureMessage");
        }

        private void CreateOpenTelemetryLogCounter()
        {
            Create.Table("OpenTelemetryLogCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(255).Nullable()
                .WithColumn("Regex").AsString(2000).Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString(255).Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString(255).Nullable()
                .WithColumn("Version").AsInt32().Nullable();

            Create.Index().OnTable("OpenTelemetryLogCounter").OnColumn("Name").Ascending();
        }

        private void CreateOpenTelemetryExclude()
        {
            Create.Table("OpenTelemetryExclude")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(255).Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString(255).Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString(255).Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString(255).Nullable()
                .WithColumn("Version").AsInt32().Nullable();

            Create.Index().OnTable("OpenTelemetryExclude").OnColumn("Name").Ascending();
        }

        private void GrantOpenTelemetryLogCounterAndExcludePermissions()
        {
            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 42,
                Name = "Read Write OpenTelemetry Log Counter"
            });
            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 42,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 43,
                Name = "Read Write OpenTelemetry Exclude"
            });
            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 43,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });
        }

        private void CreateOtlpDispatchCounter()
        {
            Create.Table("OtlpDispatchCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("SignalId").AsInt32().Nullable()
                .WithColumn("Count").AsInt64().Nullable()
                .WithColumn("SuccessCount").AsInt64().Nullable()
                .WithColumn("FailureCount").AsInt64().Nullable()
                .WithColumn("ItemCount").AsInt64().Nullable()
                .WithColumn("DroppedCount").AsInt64().Nullable()
                .WithColumn("TotalMicroseconds").AsInt64().Nullable()
                .WithColumn("MinMicroseconds").AsInt64().Nullable()
                .WithColumn("MaxMicroseconds").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("OtlpDispatchCounter", "CreatedDate");
            Create.Index().OnTable("OtlpDispatchCounter").OnColumn("SignalId").Ascending();
        }

        private void CreateModelInvokeWarning()
        {
            Create.Table("ModelInvokeWarning")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("EntityAnalysisModelName").AsString(255).Nullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().Nullable()
                .WithColumn("Message").AsString(int.MaxValue).Nullable()
                .WithColumn("ElapsedMicroseconds").AsInt64().Nullable()
                .WithColumn("SinceLastEntryMicroseconds").AsInt64().Nullable()
                .WithColumn("ThreadId").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("ModelInvokeWarning", "OccurredDate");
            Create.Index().OnTable("ModelInvokeWarning").OnColumn("EntityAnalysisModelGuid").Ascending();
            TrigramIndex("ModelInvokeWarning", "Message");
            TrigramIndex("ModelInvokeWarning", "EntityAnalysisModelName");
        }

        private void CreateArchiverStagePerformanceCounter()
        {
            Create.Table("ArchiverStagePerformanceCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("StageId").AsInt32().Nullable()
                .WithColumn("TotalMicroseconds").AsInt64().Nullable()
                .WithColumn("MinMicroseconds").AsInt64().Nullable()
                .WithColumn("MaxMicroseconds").AsInt64().Nullable()
                .WithColumn("InvokeCount").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("ArchiverStagePerformanceCounter", "CreatedDate");
            Create.Index().OnTable("ArchiverStagePerformanceCounter").OnColumn("StageId").Ascending();
            Create.Index().OnTable("ArchiverStagePerformanceCounter").OnColumn("EntityAnalysisModelGuid")
                .Ascending();
        }

        private void CreateCaseCreationStagePerformanceCounter()
        {
            Create.Table("CaseCreationStagePerformanceCounter")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("StageId").AsInt32().Nullable()
                .WithColumn("TotalMicroseconds").AsInt64().Nullable()
                .WithColumn("MinMicroseconds").AsInt64().Nullable()
                .WithColumn("MaxMicroseconds").AsInt64().Nullable()
                .WithColumn("InvokeCount").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("CaseCreationStagePerformanceCounter", "CreatedDate");
            Create.Index().OnTable("CaseCreationStagePerformanceCounter").OnColumn("StageId").Ascending();
        }

        private void CreateCaseCreationWarning()
        {
            Create.Table("CaseCreationWarning")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().Nullable()
                .WithColumn("CaseWorkflowGuid").AsGuid().Nullable()
                .WithColumn("CaseKey").AsString(255).Nullable()
                .WithColumn("CaseKeyValue").AsString(255).Nullable()
                .WithColumn("StageId").AsInt32().Nullable()
                .WithColumn("Destination").AsString(255).Nullable()
                .WithColumn("DurationMicroseconds").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("CaseCreationWarning", "OccurredDate");
            Create.Index().OnTable("CaseCreationWarning").OnColumn("TenantRegistryId").Ascending();
            Create.Index().OnTable("CaseCreationWarning").OnColumn("StageId").Ascending();
            TrigramIndex("CaseCreationWarning", "CaseKeyValue");
        }

        private void CreateArchiverWarning()
        {
            Create.Table("ArchiverWarning")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("EntityAnalysisModelName").AsString(255).Nullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().Nullable()
                .WithColumn("StageId").AsInt32().Nullable()
                .WithColumn("DurationMicroseconds").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("ArchiverWarning", "OccurredDate");
            Create.Index().OnTable("ArchiverWarning").OnColumn("EntityAnalysisModelGuid").Ascending();
            Create.Index().OnTable("ArchiverWarning").OnColumn("StageId").Ascending();
        }

        private void CreateCaptureQueueHealth()
        {
            Create.Table("CaptureQueueHealth")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("QueueId").AsInt32().Nullable()
                .WithColumn("QueueDepth").AsInt32().Nullable()
                .WithColumn("DroppedCount").AsInt64().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("CaptureQueueHealth", "CreatedDate");
            Create.Index().OnTable("CaptureQueueHealth").OnColumn("QueueId").Ascending();
        }

        private void CreateHaProxyServerStatus()
        {
            Create.Table("HAProxyServerStatus")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("PxName").AsString(64).Nullable()
                .WithColumn("SvName").AsString(64).Nullable()
                .WithColumn("Status").AsString(16).Nullable()
                .WithColumn("Addr").AsString(64).Nullable()
                .WithColumn("CheckStatus").AsString(32).Nullable()
                .WithColumn("CheckCode").AsInt32().Nullable()
                .WithColumn("ChkFail").AsInt32().Nullable()
                .WithColumn("ChkDown").AsInt32().Nullable()
                .WithColumn("LastChg").AsInt32().Nullable()
                .WithColumn("Scur").AsInt32().Nullable()
                .WithColumn("Qcur").AsInt32().Nullable()
                .WithColumn("Weight").AsInt32().Nullable()
                .WithColumn("Act").AsInt32().Nullable()
                .WithColumn("Bck").AsInt32().Nullable()
                .WithColumn("Hrsp2Xx").AsInt64().Nullable()
                .WithColumn("Hrsp5Xx").AsInt64().Nullable()
                .WithColumn("Mode").AsString(16).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("HAProxyServerStatus", "CreatedDate");
            TrigramIndex("HAProxyServerStatus", "PxName");
            TrigramIndex("HAProxyServerStatus", "SvName");
            TrigramIndex("HAProxyServerStatus", "Status");
        }

        private void CreateHaProxyReachabilityProbe()
        {
            Create.Table("HAProxyReachabilityProbe")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("Target").AsString(32).Nullable()
                .WithColumn("HAProxyAddress").AsString(64).Nullable()
                .WithColumn("Success").AsBoolean().Nullable()
                .WithColumn("ConnectMicroseconds").AsInt64().Nullable()
                .WithColumn("HttpStatusCode").AsInt32().Nullable()
                .WithColumn("ErrorMessage").AsString(1024).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("HAProxyReachabilityProbe", "CreatedDate");
            TrigramIndex("HAProxyReachabilityProbe", "Target");
            TrigramIndex("HAProxyReachabilityProbe", "HAProxyAddress");
            Create.Index().OnTable("HAProxyReachabilityProbe").OnColumn("Success").Ascending();
        }

        private void CreateOverlayNetworkTaskDrift()
        {
            Create.Table("OverlayNetworkTaskDrift")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OccurredDate").AsDateTime().Nullable()
                .WithColumn("ServiceName").AsString(64).Nullable()
                .WithColumn("DnsResolvedAddresses").AsString(1024).Nullable()
                .WithColumn("SwarmTaskAddresses").AsString(1024).Nullable()
                .WithColumn("AddressesOnlyInDns").AsString(1024).Nullable()
                .WithColumn("AddressesOnlyInSwarm").AsString(1024).Nullable()
                .WithColumn("IsConsistent").AsBoolean().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("Instance").AsString(255).Nullable();

            IndexDateColumn("OverlayNetworkTaskDrift", "CreatedDate");
            TrigramIndex("OverlayNetworkTaskDrift", "ServiceName");
            Create.Index().OnTable("OverlayNetworkTaskDrift").OnColumn("IsConsistent").Ascending();
        }

        private void IndexDateColumn(string table, string column)
        {
            Create.Index().OnTable(table).OnColumn(column).Ascending();
        }

        private void TrigramIndex(string table, string column)
        {
            Execute.Sql(
                $"CREATE INDEX IF NOT EXISTS \"IX_{table}_{column}_Trgm\" ON \"{table}\" USING gin (lower(\"{column}\") gin_trgm_ops);");
        }

        public override void Down()
        {
        }
    }
}
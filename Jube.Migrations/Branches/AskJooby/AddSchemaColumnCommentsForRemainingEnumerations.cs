/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
    [Migration(20263107640000)]
    public class AddSchemaColumnCommentsForRemainingEnumerations : Migration
    {
        public override void Up()
        {
            foreach (var table in new[]
                     {
                         "CaseWorkflowAction", "CaseWorkflowActionVersion", "CaseWorkflowForm",
                         "CaseWorkflowFormVersion", "CaseWorkflowMacro", "CaseWorkflowMacroVersion",
                         "CaseWorkflowStatus", "CaseWorkflowStatusVersion", "EntityAnalysisModelActivationRule",
                         "EntityAnalysisModelActivationRuleVersion"
                     })
            {
                Set(table, "NotificationTypeId",
                    "Channel the notification is dispatched on when notification is enabled. 1=Email, sent " +
                    "over SMTP; 2=SMS, sent over the Clickatell HTTP gateway. Only 1 selects email, so any " +
                    "other value is dispatched as SMS. NULL behaves as 1.");
            }

            Set("EntityAnalysisInlineScript", "LanguageId",
                "Language the script in Code is compiled as by Roslyn. 1=Visual Basic .NET, 2=C#. Only 2 " +
                "selects C#, so any other value, NULL included, is compiled as Visual Basic .NET.");

            Set("LanguageString", "LanguageId",
                "Foreign key to Language.Id, not an enumeration. Join to Language for the name.");

            Set("CaseNote", "PriorityId",
                "Foreign key to CaseWorkflowPriority.Id, not an enumeration. Priorities are rows each " +
                "tenant configures against a case workflow, so the meaning of a given integer differs " +
                "between workflows and between tenants. Join to CaseWorkflowPriority for the name.");

            Set("CaseNote", "ActionId",
                "Foreign key to CaseWorkflowAction.Id, not an enumeration. Actions are rows each tenant " +
                "configures against a case workflow, so the meaning of a given integer differs between " +
                "workflows and between tenants. Join to CaseWorkflowAction for the name.");

            Set("Case", "ClosedStatusId",
                "Where the case stands in the open and closed cycle. 0=Open, 1=Suspend Open, 2=Suspend " +
                "Closed, 3=Closed, 4=Suspend Bypass. Only 3 is closed, and it is the only value that sets " +
                "ClosedDate and ClosedUser; a case at 0, 1, 2 or 4 is still outstanding and counts towards " +
                "the existing case checks. A case may only be moved to 3 from 0, 1 or 2.");

            foreach (var table in new[] { "CaseWorkflow", "CaseWorkflowVersion" })
            {
                Set(table, "CaseStatusId",
                    "Carried from the baseline schema and dead: no code path reads or writes it. The " +
                    "statuses a workflow offers are rows in CaseWorkflowStatus, keyed by " +
                    "CaseWorkflowId, and a case holds its own status as Case.CaseWorkflowStatusId.");
            }

            Set("CaseEvent", "CaseEventTypeId",
                "What happened to the case. 1=Automatic Unlock, 2=Skim Case, 3=Automatic Lock, 4=Full Case " +
                "Fetch, 5=Closed, 6=Manual Lock, 7=Diary, 8=Workflow Change, 9=Status Change, 10=Diary Date " +
                "Change, 11=Rating Change, 12=Suspend Closed, 13=Suspend Open, 14=Allocate Lock, 15=Process " +
                "Expired Case. Anything else renders as Unknown. Before and After carry the old and new " +
                "values for the change the event records.");

            foreach (var table in new[] { "VisualisationRegistryDatasource", "VisualisationRegistryDatasourceVersion" })
            {
                Set(table, "VisualisationTypeId",
                    "How VisualisationText is rendered for this datasource. 1=Kendo chart built from the " +
                    "Javascript configuration in VisualisationText, 2=map Javascript, 3=HTML. Only validated " +
                    "when IncludeDisplay is set, so a datasource that feeds a grid rather than a display may " +
                    "hold 0.");
            }

            foreach (var table in new[]
                     {
                         "CachePayloadLatestRemovalBatchResponseTime", "CachePayloadRemovalBatchResponseTime",
                         "CacheTtlCounterEntryRemovalBatchResponseTime", "EntityAnalysisModelTaskPerformanceCounter"
                     })
            {
                Set(table, "TaskTypeId",
                    "Which asynchronous engine task the timing was recorded against, from the TaskType " +
                    "enumeration. 1=SanctionsAsync, 3=TtlCountersAsync, 4=AbstractionRulesWithSearchKeysAsync, " +
                    "5=CachePayloadLatestUpsertAsync, 6=CachePayloadUpsertAsync, 7=CachePayloadInsertAsync, " +
                    "8=CacheTtlCounterEntryUpsertAsync, 9=CacheTtlCounterEntryIncrementAsync, " +
                    "13=ExecuteTimeToLiveCounterIterationAsync, 15=CacheSanctionInsertAsync, " +
                    "16=ExecuteAbstractionRulesWithSearchKeyAsync, 17=BulkInsertCachePayloadRemovalBatchEntry, " +
                    "18=SortedSetRemoveReferenceDate, 19=SetRemoveAsync, 20=PublishAsync, 21=HashDecrementBytes, " +
                    "22=HashDecrementCount, 23=HashDeletePayload, 24=HashDeletePayloadBulk, " +
                    "25=AppendBulkCleanupOfPayloadGuids, 26=SortedSetRemoveReferenceDateLatest, " +
                    "28=HashDeletePayloadLatest, 30=ProcessTtlCounterDeprecation, " +
                    "31=BulkInsertTtlCounterEntryRemovalBatchResponseTime, " +
                    "32=BulkInsertCachePayloadLatestRemovalBatchEntry, 33=SortedSetLruJournalRemove, " +
                    "34=BulkTtlCounterIdempotencyRemovalBatchEntry, 35=UpsertReferenceDateAsync, " +
                    "36=InlineFunction, 37=InlineScript, 38=Gateway, 39=HttpAdaptation. 2, 10, 11, 12, 14, 27 " +
                    "and 29 are not defined and never written.");
            }

            Set("EntityAnalysisModelTaskPerformanceCounter", "DirectionId",
                "Whether the task being timed was reading from or writing to the cache. 1=Read, 2=Write.");

            Set("ExhaustiveSearchInstance", "DataSourceId",
                "Where the training data is drawn from. 0=Archive, the real processed transactions in the " +
                "archive tables; 1=the built in demonstration dataset; 2=an uploaded dataset, in which case " +
                "the rows are in ExhaustiveInstanceCsv or ExhaustiveInstanceJson. NULL behaves as 0. The " +
                "data source may only be changed while StatusId is 0 or 20.");

            foreach (var table in new[] { "ExhaustiveSearchInstance", "ExhaustiveSearchInstanceVersion" })
            {
                Set(table, "StatusId",
                    "How far the training run has got, counting up through the pipeline. 0=Awaiting Server, " +
                    "1=Query for base case data, 2=Calculating statistics for base case, 3=Normalising data " +
                    "for unsupervised learning, 4=Unsupervised training, 5=Unsupervised training recall, " +
                    "6=Unsupervised training statistics creation, 7=Query for class filter data, " +
                    "8=Statistics for class filter data, 9=Shuffling data, 10=Normalising data, 11=Over " +
                    "sampling or symmetric sampling, 12=Storing final data, 13=Correlation analysis, " +
                    "14=Multicollinearity analysis, 15=Supervised learning, 16=Finished, 17=Finished no class " +
                    "available, 18=Stopping, 19=Stopped, 20=No data found for the configured data source. " +
                    "16, 17, 19, 20 and anything above 20 are terminal; a run is only picked up by a server " +
                    "while it is still 0. Performance charts are only meaningful from 14 onwards.");
            }

            foreach (var table in new[] { "ExhaustiveInstanceCsv", "ExhaustiveInstanceJson" })
            {
                Set(table, "StatusId",
                    "Whether the uploaded dataset has been through column mapping. 0=Pending, the file is " +
                    "parsed but its columns are not yet confirmed and the row may still be deleted by " +
                    "cancelling the upload; 1=Confirmed, the dataset is mapped and may be selected as a " +
                    "training data source. Only 1 is visible to training.");
            }

            Set("EntityAnalysisModelReprocessingRuleInstance", "StatusId",
                "How far the reprocessing instance has got. 0=Not Allocated, 1=Allocated, 2=Initial Count, " +
                "3=Processing, 4=Completed, 5=Failed. An instance is only picked up by a server while it is " +
                "still 0, at which point it is set to 1.");

            Set("ExhaustiveSearchInstanceVariable", "NormalisationTypeId",
                "How the variable is scaled before it reaches the network, decided automatically from the " +
                "population statistics rather than configured. 0=none, the variable is constant because its " +
                "maximum is zero; 1=binary, already bounded between zero and one; 2=Z score standardisation " +
                "against Mean and StandardDeviation. Only 2 transforms the value at recall.");

            Set("ExhaustiveSearchInstanceVariable", "CategoricalKind",
                "Which indicator of a one hot encoded categorical field this variable is. 1=Level, a named " +
                "level held in CategoricalLevel; 2=Other, the catch all for levels outside the vocabulary; " +
                "3=Missing, the indicator for an absent value. NULL when the variable is not categorical, in " +
                "which case CategoricalFieldId, CategoricalLevel and CategoricalIndicatorIndex are NULL too.");

            foreach (var table in new[]
                         { "ExhaustiveSearchInstanceVariable", "ExhaustiveSearchInstanceCategoricalField" })
            {
                Set(table, "ProcessingTypeId",
                    "Which part of the invocation payload the value is read from at recall, and the prefix " +
                    "the variable is named with. 1=Payload, 2=Dictionary, 3=TtlCounter, 4=Sanction, " +
                    "5=Abstraction, 6=AbstractionCalculation. Anything else, NULL included, is read as " +
                    "Abstraction. Categorical fields are always 1, since only payload fields are one hot " +
                    "encoded. This is NOT the same enumeration as ArchiveKey.ProcessingTypeId, which numbers " +
                    "the same concepts differently.");
            }

            foreach (var table in new[] { "ArchiveKey", "ArchiveKeyVersion" })
            {
                Set(table, "ProcessingTypeId",
                    "Which stage of model invocation produced the archived key, written only for elements " +
                    "marked for the report table. 1=request payload field, whether extracted by XPath or " +
                    "reflected off an inline script; 3=inline function; 5=abstraction rule, and also a TTL " +
                    "counter defaulted to zero; 6=abstraction calculation; 9=HTTP adaptation; 11=activation " +
                    "rule. This is NOT the same enumeration as ExhaustiveSearchInstanceVariable." +
                    "ProcessingTypeId, which numbers the same concepts differently.");
            }
        }

        public override void Down()
        {
        }

        private void Set(string table, string column, string text)
        {
            var quoted = text.Replace("'", "''");

            Execute.Sql($"""
                         DO $$
                         BEGIN
                           IF EXISTS (SELECT 1 FROM information_schema.columns
                                      WHERE table_schema = 'public' AND table_name = '{table}'
                                        AND column_name = '{column}') THEN
                             EXECUTE format('COMMENT ON COLUMN public."{table}"."{column}" IS %L', '{quoted}');
                           END IF;
                         END $$;
                         """);
        }
    }
}
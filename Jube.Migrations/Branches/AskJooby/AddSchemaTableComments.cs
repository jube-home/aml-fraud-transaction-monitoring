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
    [Migration(20263107650000)]
    public class AddSchemaTableComments : Migration
    {
        public override void Up()
        {
            Set("ActivationWatcher",
                "Live feed of activations a model chose to publish for human watching. One row per activation on " +
                "a rule with SendToActivationWatcher set, holding the entry key and its value, the rule summary, " +
                "the elevation and the colours the user interface paints it with, plus the coordinates where the " +
                "payload carried them. Written at invocation and sampled by " +
                "EntityAnalysisModel.ActivationWatcherSample, so it is a visible sample of activity rather than a " +
                "complete record; the complete record is Archive.");

            Set("AdvancedAnalyticsCache",
                "Packed datasets the Advanced Analytics tooling has materialised and may reuse, addressed by Guid " +
                "and expiring at ExpiryDate. Data holds the packed rows rather than readable values, so read " +
                "Rows, Columns and SizeBytes for shape and go to the owning run for meaning.");

            Set("AdvancedAnalyticsInstance",
                "An Advanced Analytics configuration attached to one entity analysis model: the unit a trained " +
                "model and its training runs hang from, named and switched on or off independently of the model " +
                "itself.");

            Set("AdvancedAnalyticsTrainedModel",
                "A trained Advanced Analytics model, of the kind named in Kind, belonging to one " +
                "AdvancedAnalyticsInstance. The row itself is the identity and the pointer to whichever version " +
                "is live in ActiveVersionId; the fitted parameters are in AdvancedAnalyticsTrainedModelVersion.");

            Set("AdvancedAnalyticsTrainedModelVersion",
                "One fitted version of an Advanced Analytics model, carrying the serialised model, its variables, " +
                "its metrics and its calibrator as JSON, and naming the training run that produced it. Unlike the " +
                "other tables whose names end in Version, this is not an audit copy: these are the real " +
                "artefacts, and AdvancedAnalyticsTrainedModel.ActiveVersionId points at the one in use.");

            Set("AdvancedAnalyticsTrainingRun",
                "One execution of Advanced Analytics training, claimed by a server and heartbeating while it " +
                "works. Arguments and InputPointer say what it was asked to do and where its data came from, " +
                "Status and Progress say how far it got, and Error says why it stopped if it did.");

            Set("AdvancedAnalyticsTrainingRunCurvePoint",
                "Points of an evaluation curve produced by a training run, such as a receiver operating " +
                "characteristic, identified by Curve and Split and ordered by PointIndex. One row per plotted " +
                "point.");

            Set("AdvancedAnalyticsTrainingRunIteration",
                "Per iteration progress within one trial of a training run: training and validation loss and the " +
                "score at that iteration, which is what a learning curve is drawn from.");

            Set("AdvancedAnalyticsTrainingRunMetric",
                "Named evaluation metrics for one trial of a training run, each tied to the split it was measured " +
                "on, so the same metric name appears once per split.");

            Set("AdvancedAnalyticsTrainingRunStep",
                "The pipeline steps of a training run in order, each with its own status, timing and error, which " +
                "is what the progress display reads to say what the run is doing now.");

            Set("AdvancedAnalyticsTrainingRunTrial",
                "One candidate model within a training run, with the hyperparameters it was given and the score " +
                "it reached. Selected marks the trial that was promoted.");

            Set("AdvancedAnalyticsTrainingRunVariable",
                "The input variables of a training run as the run itself saw them: the statistics used to " +
                "normalise each one, what it imputed for a missing value, how often a value was missing, and how " +
                "important the fitted model found it.");

            Set("ApplicationLogEntry",
                "Application log lines captured from log4net into the database for the observability pages. One " +
                "row per log event, with the level, the logger that raised it, the message and any exception. " +
                "High volume and purged on a schedule, so it is a recent window rather than the whole history.");

            Set("Archive",
                "The permanent record of model invocations: one row per transaction processed, holding the whole " +
                "request and response payload as JSON along with the entry key value, the elevation reached, the " +
                "activation count and the reference date. This is the table that grows fastest in the system and " +
                "the one most analysis is built on, so never read it without a date range or a key. ArchiveKey " +
                "holds the same entry broken out into searchable columns.");

            Set("ArchiveEntityAnalysisModelAbstractionEntry",
                "Abstraction rule matches recorded against one distinct search key value during the background " +
                "search key calculation, which is what lets an abstraction be counted without replaying the " +
                "archive.");

            Set("ArchiveKey",
                "The searchable projection of an archived invocation: one row per named element marked for the " +
                "report table, with the value in whichever of the typed columns matches it. This is how the " +
                "archive is filtered and reported on without reading the JSON payload, and ProcessingTypeId says " +
                "which stage of invocation produced the element.");

            Set("ArchiveKeyVersion",
                "Superseded ArchiveKey rows, kept when an entry is replaced, for instance by reprocessing. The " +
                "live projection is in ArchiveKey.");

            Set("ArchiveTag",
                "Tags applied to an archived invocation after the fact, by a user or by the tagging pipeline. One " +
                "row per tag per entry.");

            Set("ArchiveTagVersion",
                "Superseded ArchiveTag rows. The live tags are in ArchiveTag.");

            Set("ArchiveVersion",
                "Superseded Archive rows, kept when an archived entry is rewritten, for instance by reprocessing. " +
                "The live entry is in Archive.");

            Set("ArchiverStagePerformanceCounter",
                "Timing for each stage of the archiver background task on one engine instance, aggregated into a " +
                "count, a total and a minimum and maximum in microseconds per sampling period. StageId names the " +
                "stage.");

            Set("ArchiverWarning",
                "One row per archiver stage that ran slower than its warning threshold, naming the model, the " +
                "entry and the stage, with how long it took. Written only when the threshold is breached, so an " +
                "empty table means the archiver is keeping up.");

            Set("AskJoobyAgentGuardRule",
                "One configurable row per AgentGuard rule, read by AskJoobyInjectionGuard.ResolveRule to decide " +
                "whether that rule is enabled (and, for the two topic-boundary rules, what topics it should " +
                "judge against). A rule with no row here defaults to enabled with the platform's own built-in " +
                "topics.");

            Set("AskJoobyCompaction",
                "The record of a conversation being compacted: what the compacted span said before and after, how " +
                "many messages and characters it covered, how long it took, and the embedding that makes the " +
                "original text searchable afterwards. This is what lets compaction be aggressive, because nothing " +
                "summarised away is actually lost.");

            Set("AskJoobyConversation",
                "One Ask Jooby conversation, owned by a tenant and a user. The messages are in AskJoobyMessage.");

            Set("AskJoobyDataCache",
                "Tabular tool results held out of the model's context and addressed by PointerKey. A tool that " +
                "would otherwise return thousands of rows stores them here and hands the model the pointer " +
                "instead, which is then read back through the cached data tools. Expires at ExpiresAt.");

            Set("AskJoobyFile",
                "Files attached to a conversation or produced by it, with the bytes in Object.");

            Set("AskJoobyMemory",
                "The Salt Lick: durable memories the assistant or a user has laid down, each with a scope, a kind " +
                "and an embedding so it can be recalled semantically. A memory scoped to a user is only ever " +
                "recalled for that user; a system memory is recalled for everyone in the tenant.");

            Set("AskJoobyMessage",
                "One message in a conversation, with its role, its content and an embedding for semantic search. " +
                "A row marked IsCompact is a compaction summary standing in for the span of messages up to " +
                "CompactedToAskJoobyMessageId rather than something anybody said.");

            Set("AskJoobyModelCall",
                "One call to a language model within a run, with the model used, the tokens in and out, why it " +
                "stopped, and how long it took to first chunk and in total. This is where the token cost of a " +
                "turn is measured.");

            Set("AskJoobyRagChunk",
                "The retrieval corpus: documentation and source chunked for search, each chunk carrying its " +
                "origin file and line range, its qualified name, its text and its embedding. Searched by combined " +
                "full text and vector similarity.");

            Set("AskJoobyRun",
                "One agent run, which is either the top level turn or a specialist spawned by one, linked to its " +
                "parent by ParentAskJoobyRunId. AgentType says which specialist it was and Depth how far down the " +
                "spawn tree it sat.");

            Set("AskJoobyToolCall",
                "One tool invocation within a run, with the arguments it was given, what it returned, and how " +
                "long it took.");

            Set("CacheAbstraction",
                "Postgres backed store for abstraction values keyed by search key and value, used where Redis is " +
                "not the cache. Equivalent to the Redis abstraction cache rather than an addition to it.");

            Set("CachePayload",
                "Postgres backed store of recent request payloads keyed by model and entry, used where Redis is " +
                "not the cache and read when a rule needs history for the same entity.");

            Set("CachePayloadLatest",
                "The most recent payload per entry key value for a model, which is what a rule reads when it " +
                "needs only the latest state of an entity rather than its history.");

            Set("CachePayloadLatestRemovalBatch",
                "One run of the background job that expires latest payload entries for a model, recording what " +
                "the run found and when it finished.");

            Set("CachePayloadLatestRemovalBatchEntry",
                "The individual entries one latest payload removal batch expired.");

            Set("CachePayloadLatestRemovalBatchResponseTime",
                "Timing of each cache operation a latest payload removal batch performed, by task type, which is " +
                "how the cost of expiry is attributed.");

            Set("CachePayloadRemovalBatch",
                "One run of the background job that expires payload history for a model, recording what the run " +
                "found and when it finished.");

            Set("CachePayloadRemovalBatchEntry",
                "The individual entries one payload removal batch expired.");

            Set("CachePayloadRemovalBatchResponseTime",
                "Timing of each cache operation a payload removal batch performed, by task type.");

            Set("CacheReferenceDate",
                "The high water mark reference date seen per model, which is what interval arithmetic is measured " +
                "back from when the engine is replaying rather than running live.");

            Set("CacheSanction",
                "Cached sanction match results keyed by the multipart string and the distance threshold they were " +
                "matched at, so a repeated name does not pay for the Levenshtein search twice.");

            Set("CacheSetHash",
                "Postgres backed hash and set storage standing in for Redis, partitioned across the " +
                "CacheSetHash_0 to CacheSetHash_3 tables by key. Values are held in whichever of the typed " +
                "columns matches, and the meaning of a key belongs to whatever wrote it rather than to this " +
                "table.");

            Set("CacheSetHash_0",
                "One partition of CacheSetHash, selected by a hash of the key. See CacheSetHash.");

            Set("CacheSetHash_1",
                "One partition of CacheSetHash, selected by a hash of the key. See CacheSetHash.");

            Set("CacheSetHash_2",
                "One partition of CacheSetHash, selected by a hash of the key. See CacheSetHash.");

            Set("CacheSetHash_3",
                "One partition of CacheSetHash, selected by a hash of the key. See CacheSetHash.");

            Set("CacheTtlCounter",
                "The current value of each time to live counter for a model, keyed by the counter definition and " +
                "the data value it counts for.");

            Set("CacheTtlCounterEntry",
                "The individual dated entries behind a time to live counter, which is what allows the counter to " +
                "be decremented as entries age out rather than recalculated.");

            Set("CacheTtlCounterEntryRemovalBatch",
                "One run of the background job that ages out time to live counter entries, recording what it " +
                "found and when it finished.");

            Set("CacheTtlCounterEntryRemovalBatchEntry",
                "The individual entries one counter removal batch expired, with the decrement applied and the " +
                "count that remained.");

            Set("CacheTtlCounterEntryRemovalBatchResponseTime",
                "Timing of each cache operation a counter removal batch performed, by task type.");

            Set("CaptureQueueHealth",
                "Depth and dropped count for each in memory capture queue feeding the observability tables, " +
                "sampled per instance. A rising DroppedCount means samples are being discarded because the writer " +
                "cannot keep up, so the observability tables are incomplete for that period.");

            Set("Case",
                "A case raised against one archived invocation and worked by a human, identified by CaseKey and " +
                "CaseKeyValue -- the search key name and the value it was raised against, and the pair every " +
                "case lookup tool (CaseByCaseKeyValueGet, CaseNoteListByCaseKeyValue, CaseEventByCaseKeyValueGet, " +
                "CaseFileListByCaseKeyValue and the rest) is addressed by. This identity lives here, not on " +
                "CaseWorkflow. Beyond the identity: who has it locked, where it sits in the open and closed " +
                "cycle, its rating, its diary date, and the workflow and status it belongs to. The payload is " +
                "denormalised into Json so a case can be read without the archive.");

            Set("CaseCreationStagePerformanceCounter",
                "Timing for each stage of case creation on one instance, aggregated per sampling period. StageId " +
                "names the stage.");

            Set("CaseCreationWarning",
                "One row per case creation stage that ran slower than its warning threshold, naming the workflow " +
                "and the case key. Written only on breach.");

            Set("CaseEvent",
                "The audit trail of a case: one row per thing that happened to it, with the before and after " +
                "values where the event records a change. CaseEventTypeId says what happened.");

            Set("CaseExecutionLog",
                "One row per case search executed against the database, with the command, how long it took, how " +
                "many rows came back and the error if it failed. This is where a slow case filter shows up.");

            Set("CaseFile",
                "Files attached to a case, with the bytes in Object.");

            Set("CaseNote",
                "Notes added to a case by a user, optionally carrying the action taken and the priority set at " +
                "the time. ActionId and PriorityId are references to the workflow's own configured rows, not " +
                "fixed enumerations.");

            Set("CaseWorkflow",
                "A case workflow: the container that defines how cases raised from one entity analysis model are " +
                "presented and worked, including the statuses, actions, filters, forms, macros and displays that " +
                "hang from it. This is the workflow definition, not a case itself, and it carries no CaseKey or " +
                "CaseKeyValue of its own -- that identity lives on Case, one row per actual case, each pointing " +
                "back here by CaseWorkflowGuid.");

            Set("CaseWorkflowAction",
                "An action a user may record against a case in this workflow, optionally calling an HTTP endpoint " +
                "and sending a notification when it is used.");

            Set("CaseWorkflowActionRole",
                "Grants one role sight of one CaseWorkflowAction. This is an allow-list and not a restriction " +
                "list: a CaseWorkflowAction with no row here is visible to nobody, because every query that reads " +
                "it requires a matching row for a role the caller holds. Joins CaseWorkflowAction by " +
                "CaseWorkflowActionGuid and RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, so " +
                "that a configuration export carries the grant with it.");

            Set("CaseWorkflowActionVersion",
                "Audit history for CaseWorkflowAction. On update the superseded CaseWorkflowAction row is copied " +
                "here before the live row is overwritten, so this holds previous states and never the current " +
                "one, and the live row carries a Version one ahead of the newest row here. CaseWorkflowActionId " +
                "points back at the live row. See CaseWorkflowAction for what the columns mean.");

            Set("CaseWorkflowDisplay",
                "A block of HTML shown as part of a case in this workflow, which is how a workflow presents " +
                "payload content in a shape of its own choosing.");

            Set("CaseWorkflowDisplayRole",
                "Grants one role sight of one CaseWorkflowDisplay. This is an allow-list and not a restriction " +
                "list: a CaseWorkflowDisplay with no row here is visible to nobody, because every query that " +
                "reads it requires a matching row for a role the caller holds. Joins CaseWorkflowDisplay by " +
                "CaseWorkflowDisplayGuid and RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, so " +
                "that a configuration export carries the grant with it.");

            Set("CaseWorkflowDisplayVersion",
                "Audit history for CaseWorkflowDisplay. On update the superseded CaseWorkflowDisplay row is " +
                "copied here before the live row is overwritten, so this holds previous states and never the " +
                "current one, and the live row carries a Version one ahead of the newest row here. " +
                "CaseWorkflowDisplayId points back at the live row. See CaseWorkflowDisplay for what the columns " +
                "mean.");

            Set("CaseWorkflowFilter",
                "A saved filter over the cases of one workflow, held both as the builder's JSON and as the SQL " +
                "compiled from it, optionally with a visualisation attached.");

            Set("CaseWorkflowFilterRole",
                "Grants one role sight of one CaseWorkflowFilter. This is an allow-list and not a restriction " +
                "list: a CaseWorkflowFilter with no row here is visible to nobody, because every query that reads " +
                "it requires a matching row for a role the caller holds. Joins CaseWorkflowFilter by " +
                "CaseWorkflowFilterGuid and RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, so " +
                "that a configuration export carries the grant with it.");

            Set("CaseWorkflowFilterVersion",
                "Audit history for CaseWorkflowFilter. On update the superseded CaseWorkflowFilter row is copied " +
                "here before the live row is overwritten, so this holds previous states and never the current " +
                "one, and the live row carries a Version one ahead of the newest row here. CaseWorkflowFilterId " +
                "points back at the live row. See CaseWorkflowFilter for what the columns mean.");

            Set("CaseWorkflowForm",
                "A form a user may complete against a case in this workflow, defined as HTML, optionally calling " +
                "an HTTP endpoint and sending a notification on submission.");

            Set("CaseWorkflowFormEntry",
                "One completed form against one case, with the submitted payload. The individual field values are " +
                "broken out into CaseWorkflowFormEntryValue.");

            Set("CaseWorkflowFormEntryValue",
                "One field of one completed form, broken out by name and value so it can be reported on without " +
                "parsing the payload.");

            Set("CaseWorkflowFormRole",
                "Grants one role sight of one CaseWorkflowForm. This is an allow-list and not a restriction list: " +
                "a CaseWorkflowForm with no row here is visible to nobody, because every query that reads it " +
                "requires a matching row for a role the caller holds. Joins CaseWorkflowForm by " +
                "CaseWorkflowFormGuid and RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, so " +
                "that a configuration export carries the grant with it.");

            Set("CaseWorkflowFormVersion",
                "Audit history for CaseWorkflowForm. On update the superseded CaseWorkflowForm row is copied here " +
                "before the live row is overwritten, so this holds previous states and never the current one, and " +
                "the live row carries a Version one ahead of the newest row here. CaseWorkflowFormId points back " +
                "at the live row. See CaseWorkflowForm for what the columns mean.");

            Set("CaseWorkflowMacro",
                "A Javascript macro a user may run against a case in this workflow, optionally calling an HTTP " +
                "endpoint and sending a notification when it runs.");

            Set("CaseWorkflowMacroRole",
                "Grants one role sight of one CaseWorkflowMacro. This is an allow-list and not a restriction " +
                "list: a CaseWorkflowMacro with no row here is visible to nobody, because every query that reads " +
                "it requires a matching row for a role the caller holds. Joins CaseWorkflowMacro by " +
                "CaseWorkflowMacroGuid and RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, so " +
                "that a configuration export carries the grant with it.");

            Set("CaseWorkflowMacroVersion",
                "Audit history for CaseWorkflowMacro. On update the superseded CaseWorkflowMacro row is copied " +
                "here before the live row is overwritten, so this holds previous states and never the current " +
                "one, and the live row carries a Version one ahead of the newest row here. CaseWorkflowMacroId " +
                "points back at the live row. See CaseWorkflowMacro for what the columns mean.");

            Set("CaseWorkflowRole",
                "Grants one role sight of one CaseWorkflow. This is an allow-list and not a restriction list: a " +
                "CaseWorkflow with no row here is visible to nobody, because every query that reads it requires a " +
                "matching row for a role the caller holds. Joins CaseWorkflow by CaseWorkflowGuid and " +
                "RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, so that a configuration export " +
                "carries the grant with it.");

            Set("CaseWorkflowStatus",
                "A status a case in this workflow may hold, with the colours it is painted in and its ordering " +
                "priority, optionally calling an HTTP endpoint and sending a notification when a case moves into " +
                "it.");

            Set("CaseWorkflowStatusRole",
                "Grants one role sight of one CaseWorkflowStatus. This is an allow-list and not a restriction " +
                "list: a CaseWorkflowStatus with no row here is visible to nobody, because every query that reads " +
                "it requires a matching row for a role the caller holds. Joins CaseWorkflowStatus by " +
                "CaseWorkflowStatusGuid and RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, so " +
                "that a configuration export carries the grant with it.");

            Set("CaseWorkflowStatusVersion",
                "Audit history for CaseWorkflowStatus. On update the superseded CaseWorkflowStatus row is copied " +
                "here before the live row is overwritten, so this holds previous states and never the current " +
                "one, and the live row carries a Version one ahead of the newest row here. CaseWorkflowStatusId " +
                "points back at the live row. See CaseWorkflowStatus for what the columns mean.");

            Set("CaseWorkflowVersion",
                "Audit history for CaseWorkflow. On update the superseded CaseWorkflow row is copied here before " +
                "the live row is overwritten, so this holds previous states and never the current one, and the " +
                "live row carries a Version one ahead of the newest row here. CaseWorkflowId points back at the " +
                "live row. See CaseWorkflow for what the columns mean.");

            Set("CaseWorkflowXPath",
                "One element lifted out of the case payload for display in this workflow's case list and case " +
                "page, with the conditional formatting rules that colour it.");

            Set("CaseWorkflowXPathRole",
                "Grants one role sight of one CaseWorkflowXPath. This is an allow-list and not a restriction " +
                "list: a CaseWorkflowXPath with no row here is visible to nobody, because every query that reads " +
                "it requires a matching row for a role the caller holds. Joins CaseWorkflowXPath by " +
                "CaseWorkflowXPathGuid and RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, so " +
                "that a configuration export carries the grant with it.");

            Set("CaseWorkflowXPathVersion",
                "Audit history for CaseWorkflowXPath. On update the superseded CaseWorkflowXPath row is copied " +
                "here before the live row is overwritten, so this holds previous states and never the current " +
                "one, and the live row carries a Version one ahead of the newest row here. CaseWorkflowXPathId " +
                "points back at the live row. See CaseWorkflowXPath for what the columns mean.");

            Set("ContainerLogEntry",
                "Log lines captured from container standard output and standard error for the observability " +
                "pages, one row per line, with the container it came from.");

            Set("Country",
                "Reference list of countries with their ISO codes and a representative coordinate. Static " +
                "reference data, not tenant owned.");

            Set("Currency",
                "Reference list of currencies with a symbol and an exchange rate to the base currency. Static " +
                "reference data, not tenant owned.");

            Set("DictionaryEvalExpression",
                "Named expressions available to the rule dictionary, with the compiled form and any compilation " +
                "error. Static platform data rather than tenant configuration.");

            Set("DockerContainerMetric",
                "A sample of one container's state and resource use at a point in time, for the infrastructure " +
                "observability pages.");

            Set("DockerEvent",
                "Docker daemon events as they occurred, naming the actor and the action, for the infrastructure " +
                "observability pages.");

            Set("DockerHostMetric",
                "A sample of the Docker host as a whole: its version and capacity, how many containers are in " +
                "each state, and how much disk the images, containers and volumes occupy.");

            Set("DotNetRuntimeMetric",
                "A sample of one process's .NET runtime: garbage collection counts, heap and working set, thread " +
                "pool availability and queue length, lock contention, and the container limits it is running " +
                "under. This is where a thread pool starvation or a memory problem is seen.");

            Set("DynamicSqlQueryLog",
                "The audit of every dynamic SQL statement Ask Jooby has run or had rejected, recording the " +
                "statement as it was actually rendered after table substitution rather than as the model wrote " +
                "it, who ran it, whether it succeeded, how long it took and how many rows it returned.");

            Set("DynamicSqlTokens",
                "Configuration rows for the dynamic SQL table allow-list, naming a table and the permissions " +
                "required to query it.");

            Set("EntityAnalysisAsynchronousQueueBalance",
                "Depth of each asynchronous engine queue on one instance at a point in time, which is how a " +
                "backlog in callbacks, case creation, tagging or notification is seen before it becomes a " +
                "timeout.");

            Set("EntityAnalysisInlineScript",
                "A reusable inline script, written in Visual Basic .NET or C# and compiled by Roslyn, that models " +
                "may attach. The compiled flag and any compilation error are recorded here; the attachment to a " +
                "model is EntityAnalysisModelInlineScript.");

            Set("EntityAnalysisInstance",
                "One engine process that has started, identified by a Guid and the instance name it runs under. " +
                "Everything named Instance elsewhere in the observability tables refers back to this.");

            Set("EntityAnalysisModel",
                "The central configuration object: one entity analysis model, defining how a transaction payload " +
                "is read, which caches and archives are enabled, what the response elevation limits are, and " +
                "every switch that governs invocation. Almost every other configuration table in the system hangs " +
                "from this one.");

            Set("EntityAnalysisModelAbstractionCalculation",
                "A calculation over abstraction values, written as a function script, producing a further named " +
                "value rules may predicate on.");

            Set("EntityAnalysisModelAbstractionCalculationVersion",
                "Audit history for EntityAnalysisModelAbstractionCalculation. On update the superseded " +
                "EntityAnalysisModelAbstractionCalculation row is copied here before the live row is overwritten, " +
                "so this holds previous states and never the current one, and the live row carries a Version one " +
                "ahead of the newest row here. EntityAnalysisModelAbstractionCalculationId points back at the " +
                "live row. See EntityAnalysisModelAbstractionCalculation for what the columns mean.");

            Set("EntityAnalysisModelAbstractionRule",
                "An abstraction: a rule evaluated over the entity's own recent history rather than the " +
                "transaction in front of it, defined by a search key, an interval and an aggregation function. " +
                "These are the counters most fraud rules are actually built from.");

            Set("EntityAnalysisModelAbstractionRuleVersion",
                "Audit history for EntityAnalysisModelAbstractionRule. On update the superseded " +
                "EntityAnalysisModelAbstractionRule row is copied here before the live row is overwritten, so " +
                "this holds previous states and never the current one, and the live row carries a Version one " +
                "ahead of the newest row here. EntityAnalysisModelAbstractionRuleId points back at the live row. " +
                "See EntityAnalysisModelAbstractionRule for what the columns mean.");

            Set("EntityAnalysisModelActivationRule",
                "An activation rule: the rule that decides whether a transaction is acted on, carrying the " +
                "response elevation it raises, the case it opens, the counter it increments, the notification it " +
                "sends and the override and bypass behaviour around it. The running counts of evaluations and " +
                "activations are kept on the row.");

            Set("EntityAnalysisModelActivationRuleCounterHistory",
                "Periodic snapshots of an activation rule's evaluation and activation counters per instance, " +
                "which is what the activation rate over time is drawn from.");

            Set("EntityAnalysisModelActivationRuleOverride",
                "An override of one named activation rule for one key value, suppressing or forcing it until " +
                "DeleteExpiryDate. OverrideKind says which.");

            Set("EntityAnalysisModelActivationRuleOverrideVersion",
                "Audit history for EntityAnalysisModelActivationRuleOverride. On update the superseded " +
                "EntityAnalysisModelActivationRuleOverride row is copied here before the live row is overwritten, " +
                "so this holds previous states and never the current one, and the live row carries a Version one " +
                "ahead of the newest row here. EntityAnalysisModelActivationRuleOverrideId points back at the " +
                "live row. See EntityAnalysisModelActivationRuleOverride for what the columns mean.");

            Set("EntityAnalysisModelActivationRuleVersion",
                "Audit history for EntityAnalysisModelActivationRule. On update the superseded " +
                "EntityAnalysisModelActivationRule row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelActivationRuleId points back at the live row. See " +
                "EntityAnalysisModelActivationRule for what the columns mean.");

            Set("EntityAnalysisModelAsynchronousQueueBalance",
                "Depth of the archive and activation watcher queues for one model on one instance at a point in " +
                "time.");

            Set("EntityAnalysisModelBacktestInstance",
                "One backtest of a rule against archived data, claimed by a server and heartbeating while it " +
                "works, recording how much was scanned and evaluated and what the result was.");

            Set("EntityAnalysisModelDictionary",
                "A named lookup available to rules, whose entries are in EntityAnalysisModelDictionaryKvp. " +
                "DataName says which payload element is looked up.");

            Set("EntityAnalysisModelDictionaryCsvFileUpload",
                "One upload of a CSV file into a dictionary, recording how many records and errors it produced.");

            Set("EntityAnalysisModelDictionaryKvp",
                "One key and value pair in a dictionary, optionally expiring at DeleteExpiryDate.");

            Set("EntityAnalysisModelDictionaryKvpVersion",
                "Audit history for EntityAnalysisModelDictionaryKvp. On update the superseded " +
                "EntityAnalysisModelDictionaryKvp row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelDictionaryKvpId points back at the live row. See " +
                "EntityAnalysisModelDictionaryKvp for what the columns mean.");

            Set("EntityAnalysisModelDictionaryVersion",
                "Audit history for EntityAnalysisModelDictionary. On update the superseded " +
                "EntityAnalysisModelDictionary row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelDictionaryId points back at the live row. See " +
                "EntityAnalysisModelDictionary for what the columns mean.");

            Set("EntityAnalysisModelEngineSnapshot",
                "The whole of one model's synchronised configuration as the engine holds it, written per " +
                "instance, which is how what a running engine actually believes can be compared with what the " +
                "database says.");

            Set("EntityAnalysisModelGatewayRule",
                "A gateway rule: evaluated before the model does any work, to decide whether this transaction is " +
                "processed at all and at what maximum elevation. The cheapest place to stop traffic.");

            Set("EntityAnalysisModelGatewayRuleCounterHistory",
                "Periodic snapshots of a gateway rule's evaluation and activation counters per instance.");

            Set("EntityAnalysisModelGatewayRuleVersion",
                "Audit history for EntityAnalysisModelGatewayRule. On update the superseded " +
                "EntityAnalysisModelGatewayRule row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelGatewayRuleId points back at the live row. See " +
                "EntityAnalysisModelGatewayRule for what the columns mean.");

            Set("EntityAnalysisModelHttpAdaptation",
                "An external HTTP call made during invocation whose response becomes a named value rules may " +
                "predicate on, ordered by Priority.");

            Set("EntityAnalysisModelHttpAdaptationVersion",
                "Audit history for EntityAnalysisModelHttpAdaptation. On update the superseded " +
                "EntityAnalysisModelHttpAdaptation row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelHttpAdaptationId points back at the live row. See " +
                "EntityAnalysisModelHttpAdaptation for what the columns mean.");

            Set("EntityAnalysisModelInlineFunction",
                "A function script evaluated during invocation, producing a named, typed value rules may " +
                "predicate on, optionally encrypted at rest.");

            Set("EntityAnalysisModelInlineFunctionVersion",
                "Audit history for EntityAnalysisModelInlineFunction. On update the superseded " +
                "EntityAnalysisModelInlineFunction row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelInlineFunctionId points back at the live row. See " +
                "EntityAnalysisModelInlineFunction for what the columns mean.");

            Set("EntityAnalysisModelInlineScript",
                "The attachment of a reusable EntityAnalysisInlineScript to one model, which is what makes that " +
                "script's public properties available as payload values.");

            Set("EntityAnalysisModelInlineScriptVersion",
                "Audit history for EntityAnalysisModelInlineScript. On update the superseded " +
                "EntityAnalysisModelInlineScript row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelInlineScriptId points back at the live row. See " +
                "EntityAnalysisModelInlineScript for what the columns mean.");

            Set("EntityAnalysisModelInstance",
                "The pairing of one model with one running engine instance, which is what lets per model, per " +
                "instance counters be attributed.");

            Set("EntityAnalysisModelList",
                "A named list available to rules, whose values are in EntityAnalysisModelListValue.");

            Set("EntityAnalysisModelListCsvFileUpload",
                "One upload of a CSV file into a list, recording how many records and errors it produced.");

            Set("EntityAnalysisModelListValue",
                "One value in a list, optionally expiring at DeleteExpiryDate.");

            Set("EntityAnalysisModelListValueVersion",
                "Audit history for EntityAnalysisModelListValue. On update the superseded " +
                "EntityAnalysisModelListValue row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelListValueId points back at the live row. See " +
                "EntityAnalysisModelListValue for what the columns mean.");

            Set("EntityAnalysisModelListVersion",
                "Audit history for EntityAnalysisModelList. On update the superseded EntityAnalysisModelList row " +
                "is copied here before the live row is overwritten, so this holds previous states and never the " +
                "current one, and the live row carries a Version one ahead of the newest row here. " +
                "EntityAnalysisModelListId points back at the live row. See EntityAnalysisModelList for what the " +
                "columns mean.");

            Set("EntityAnalysisModelOverride",
                "An override of a whole model for one key value, suppressing or forcing processing until " +
                "DeleteExpiryDate. OverrideKind says which.");

            Set("EntityAnalysisModelOverrideVersion",
                "Audit history for EntityAnalysisModelOverride. On update the superseded " +
                "EntityAnalysisModelOverride row is copied here before the live row is overwritten, so this holds " +
                "previous states and never the current one, and the live row carries a Version one ahead of the " +
                "newest row here. EntityAnalysisModelOverrideId points back at the live row. See " +
                "EntityAnalysisModelOverride for what the columns mean.");

            Set("EntityAnalysisModelProcessingCounter",
                "The headline throughput counters for one model on one instance per sampling period: invocations, " +
                "gateway matches, elevations and the limits that were hit, the implicit asynchronous outcomes, " +
                "and the fastest and slowest response. This is the table a throughput or latency question should " +
                "start from.");

            Set("EntityAnalysisModelReprocessingRule",
                "A rule selecting archived transactions to be put back through the model, with the sample and the " +
                "interval it reaches over.");

            Set("EntityAnalysisModelReprocessingRuleInstance",
                "One run of a reprocessing rule, claimed by a server, recording how much was available, sampled, " +
                "matched and processed, and where it got to.");

            Set("EntityAnalysisModelRequestXpath",
                "One element extracted from the request payload: its name, its datatype, the path it is read " +
                "from, whether it is a search key, how it is cached and whether it is encrypted at rest. " +
                "Everything a rule can see from the transaction itself is defined here.");

            Set("EntityAnalysisModelRequestXpathVersion",
                "Audit history for EntityAnalysisModelRequestXpath. On update the superseded " +
                "EntityAnalysisModelRequestXpath row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelRequestXpathId points back at the live row. See " +
                "EntityAnalysisModelRequestXpath for what the columns mean.");

            Set("EntityAnalysisModelResponseTimePipelineCounter",
                "Timing and allocation for each stage of the invocation pipeline in sequence, for one model on " +
                "one instance, which is how the time inside a single invocation is attributed stage by stage.");

            Set("EntityAnalysisModelRole",
                "Grants one role sight of one EntityAnalysisModel. This is an allow-list and not a restriction " +
                "list: a EntityAnalysisModel with no row here is visible to nobody, because every query that " +
                "reads it requires a matching row for a role the caller holds. Joins EntityAnalysisModel by " +
                "EntityAnalysisModelGuid and RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, so " +
                "that a configuration export carries the grant with it.");

            Set("EntityAnalysisModelSampleExecutionLog",
                "One run of the model sampling query over the archive, with the range it covered, how long it " +
                "took and the error if it failed.");

            Set("EntityAnalysisModelSanction",
                "A sanction screening configuration for one model: which multipart string is screened, at what " +
                "Levenshtein distance, how the matches are aggregated into a single value and how long a result " +
                "is cached.");

            Set("EntityAnalysisModelSanctionVersion",
                "Audit history for EntityAnalysisModelSanction. On update the superseded " +
                "EntityAnalysisModelSanction row is copied here before the live row is overwritten, so this holds " +
                "previous states and never the current one, and the live row carries a Version one ahead of the " +
                "newest row here. EntityAnalysisModelSanctionId points back at the live row. See " +
                "EntityAnalysisModelSanction for what the columns mean.");

            Set("EntityAnalysisModelSearchKeyCalculationInstance",
                "One background pass over a search key for a model, counting its distinct values and expiring " +
                "stale cache, which is the work that keeps abstraction rules answerable without scanning the " +
                "archive.");

            Set("EntityAnalysisModelSearchKeyDistinctValueCalculationInstance",
                "One distinct value of a search key within a calculation pass, with its entry count and when its " +
                "abstraction matches were last refreshed.");

            Set("EntityAnalysisModelStagePerformanceCounter",
                "Timing for each stage of model invocation on one instance, aggregated per sampling period. " +
                "StageId names the stage.");

            Set("EntityAnalysisModelSynchronisationError",
                "Errors raised while an engine was synchronising its configuration, by step.");

            Set("EntityAnalysisModelSynchronisationNodeStatusEntry",
                "The heartbeat and last synchronised time of each engine node for a tenant, which is how a node " +
                "that has stopped taking configuration changes is spotted.");

            Set("EntityAnalysisModelSynchronisationSchedule",
                "A request for the engines of a tenant to resynchronise their configuration at a given time.");

            Set("EntityAnalysisModelTag",
                "A tag that may be applied to an archived invocation of this model.");

            Set("EntityAnalysisModelTagVersion",
                "Audit history for EntityAnalysisModelTag. On update the superseded EntityAnalysisModelTag row is " +
                "copied here before the live row is overwritten, so this holds previous states and never the " +
                "current one, and the live row carries a Version one ahead of the newest row here. " +
                "EntityAnalysisModelTagId points back at the live row. See EntityAnalysisModelTag for what the " +
                "columns mean.");

            Set("EntityAnalysisModelTaskPerformanceCounter",
                "Timing and allocation for each asynchronous cache task a model performed, split by whether it " +
                "was reading or writing. This is where Redis or Postgres cache cost is attributed task by task.");

            Set("EntityAnalysisModelTtlCounter",
                "A time to live counter: a count of occurrences of a data value over a rolling interval, " +
                "maintained incrementally rather than queried, which is what lets velocity rules run at " +
                "invocation speed.");

            Set("EntityAnalysisModelTtlCounterVersion",
                "Audit history for EntityAnalysisModelTtlCounter. On update the superseded " +
                "EntityAnalysisModelTtlCounter row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. EntityAnalysisModelTtlCounterId points back at the live row. See " +
                "EntityAnalysisModelTtlCounter for what the columns mean.");

            Set("EntityAnalysisModelVersion",
                "Audit history for EntityAnalysisModel. On update the superseded EntityAnalysisModel row is " +
                "copied here before the live row is overwritten, so this holds previous states and never the " +
                "current one, and the live row carries a Version one ahead of the newest row here. " +
                "EntityAnalysisModelId points back at the live row. See EntityAnalysisModel for what the columns " +
                "mean.");

            Set("EtcdClusterEvent",
                "Changes observed in the etcd cluster backing Patroni, with the previous and new value, for the " +
                "cluster orchestration pages.");

            Set("EtcdMemberStatus",
                "A sample of one etcd member: its health, its raft state and indexes, proposal counts, fsync and " +
                "commit latency, and any alarms raised.");

            Set("ExampleCaseVolumeEntry",
                "Example data behind a shipped demonstration visualisation. Not operational data.");

            Set("ExampleCustomerCaseManagement",
                "Example data behind a shipped demonstration visualisation. Not operational data.");

            Set("ExampleGeoCoded",
                "Example data behind a shipped demonstration visualisation. Not operational data.");

            Set("ExhaustiveInstanceCsv",
                "An uploaded CSV dataset that may be used to train an Exhaustive model instead of the archive, " +
                "with the parsed column definitions, the chosen dependent column and a profile of the file. The " +
                "rows are in ExhaustiveInstanceCsvRow.");

            Set("ExhaustiveInstanceCsvRow",
                "One row of an uploaded CSV dataset, held as JSON.");

            Set("ExhaustiveInstanceJson",
                "An uploaded JSON dataset that may be used to train an Exhaustive model instead of the archive, " +
                "with the parsed column definitions, the chosen dependent column and a profile of the file. The " +
                "rows are in ExhaustiveInstanceJsonRow.");

            Set("ExhaustiveInstanceJsonRow",
                "One row of an uploaded JSON dataset, held as JSON.");

            Set("ExhaustiveSearchInstance",
                "One Exhaustive model: a neural network searched for automatically over the model's own data, " +
                "carrying where its training data comes from, the filter and anomaly settings it was given, how " +
                "far training has got and the score of the best topology found. The fitted network is in " +
                "ExhaustiveSearchInstancePromotedTrialInstance.");

            Set("ExhaustiveSearchInstanceCategoricalField",
                "A categorical payload field one hot encoded for an Exhaustive model, with the vocabulary of " +
                "levels it was fitted against. Each resulting indicator becomes its own " +
                "ExhaustiveSearchInstanceVariable.");

            Set("ExhaustiveSearchInstanceData",
                "The prepared training matrix for an Exhaustive model: the dependent value, the anomaly score and " +
                "the independent values for one row, after filtering, normalisation and sampling.");

            Set("ExhaustiveSearchInstancePrescriptionHistogram",
                "The distribution of one variable of a promoted Exhaustive model, binned, which is what the " +
                "prescription display is drawn from.");

            Set("ExhaustiveSearchInstancePromotedTrialInstance",
                "The trial that was promoted to be the live Exhaustive model, with its confusion matrix, its " +
                "score, the serialised network and everything about how its probabilities were calibrated and " +
                "why.");

            Set("ExhaustiveSearchInstancePromotedTrialInstancePredictedActual",
                "Predicted against actual for one row under the promoted model, which is what the calibration and " +
                "performance plots are drawn from.");

            Set("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVar",
                "The input values behind one predicted against actual row, so an individual prediction can be " +
                "explained.");

            Set("ExhaustiveSearchInstancePromotedTrialInstanceRoc",
                "The receiver operating characteristic of the promoted model: the confusion matrix at each " +
                "threshold.");

            Set("ExhaustiveSearchInstancePromotedTrialInstanceSensitivity",
                "How sensitive the promoted model is to each of its variables, with the rank that orders them, " +
                "which is the closest thing the model has to a variable importance.");

            Set("ExhaustiveSearchInstancePromotedTrialInstanceVariable",
                "The population statistics of one variable as the promoted model holds them, which is what its " +
                "values are normalised against at recall.");

            Set("ExhaustiveSearchInstanceTrialInstance",
                "One candidate topology tried during an Exhaustive search, with its score and the serialised " +
                "network.");

            Set("ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial",
                "One activation function tried for a candidate topology.");

            Set("ExhaustiveSearchInstanceTrialInstanceSensitivity",
                "How sensitive one candidate topology is to one of its variables.");

            Set("ExhaustiveSearchInstanceTrialInstanceTopologyTrial",
                "One layer and neuron count tried during the topology search, with the score it reached and how " +
                "many trials had passed without improvement.");

            Set("ExhaustiveSearchInstanceTrialInstanceVariable",
                "The variables one candidate topology was given, in sequence, with Removed marking those the " +
                "search dropped.");

            Set("ExhaustiveSearchInstanceVariable",
                "One input variable of an Exhaustive model, with the population statistics, correlation and " +
                "binning calculated for it during training, how it is normalised, and, where it is a one hot " +
                "indicator, which categorical field and level it stands for.");

            Set("ExhaustiveSearchInstanceVariableAnomaly",
                "The population statistics of one variable restricted to the anomalous rows, for comparison " +
                "against the whole population.");

            Set("ExhaustiveSearchInstanceVariableClassification",
                "The population statistics of one variable restricted to the classified rows, for comparison " +
                "against the whole population.");

            Set("ExhaustiveSearchInstanceVariableHistogram",
                "The binned distribution of one variable over the whole training population.");

            Set("ExhaustiveSearchInstanceVariableHistogramAnomaly",
                "The binned distribution of one variable over the anomalous rows.");

            Set("ExhaustiveSearchInstanceVariableHistogramClass",
                "The binned distribution of one variable within one class label, which is what a class separation " +
                "plot is drawn from.");

            Set("ExhaustiveSearchInstanceVariableHistogramClassification",
                "The binned distribution of one variable over the classified rows.");

            Set("ExhaustiveSearchInstanceVariableMultiCollinearity",
                "The correlation between one pair of Exhaustive variables, which is what the multicollinearity " +
                "stage uses to drop redundant inputs.");

            Set("ExhaustiveSearchInstanceVersion",
                "Audit history for ExhaustiveSearchInstance. On update the superseded ExhaustiveSearchInstance " +
                "row is copied here before the live row is overwritten, so this holds previous states and never " +
                "the current one, and the live row carries a Version one ahead of the newest row here. " +
                "ExhaustiveSearchInstanceId points back at the live row. See ExhaustiveSearchInstance for what " +
                "the columns mean.");

            Set("Export",
                "One configuration export, holding the exported bytes and their encrypted form, with the error if " +
                "it failed.");

            Set("ExportPeek",
                "A readable preview of an export's contents, so what an export holds can be inspected before it " +
                "is imported anywhere.");

            Set("HAProxyReachabilityProbe",
                "One probe of a target through HAProxy, with whether it connected, how long it took and the " +
                "status it got back.");

            Set("HAProxyServerStatus",
                "A sample of one HAProxy backend server: its status, health check result, current sessions and " +
                "queue, and its response code counts.");

            Set("HashCacheAssemblyInstance",
                "The compiled assembly cache held by one engine instance, with how many entries it holds and how " +
                "many bytes they occupy.");

            Set("HashCacheAssemblyInstanceEntry",
                "One compiled assembly in the cache, keyed by the hash of the script that produced it, holding " +
                "both the source and the compiled binary so a restart need not recompile.");

            Set("HashCacheAssemblyInstanceJournal",
                "Periodic samples of an assembly cache's size and count, which is how its growth is seen.");

            Set("HttpProcessingCounter",
                "Request counts by endpoint family for one instance per sampling period, which is the front door " +
                "view of load.");

            Set("HttpResponseHeader",
                "Response headers the application adds to every HTTP response. Configuration, not observability.");

            Set("Import",
                "One configuration import, holding the bytes imported and the export they came from, with the " +
                "error if it failed.");

            Set("Language",
                "The languages available for interface strings. Referenced by LanguageString.");

            Set("LanguageString",
                "One translated interface string, keyed by LanguageStringKey within a language.");

            Set("LocalCacheInstance",
                "The in process local cache held by one engine instance, with how it filled at startup and its " +
                "current size against the managed heap.");

            Set("LocalCacheInstanceKey",
                "Per key statistics for one local cache: requests, misses, the cost of going remote on a miss, " +
                "and the subscription and removal activity behind keeping it coherent.");

            Set("LocalCacheInstanceLru",
                "Periodic samples of a local cache's least recently used accounting: what was requested, added, " +
                "removed, evicted and updated, in both bytes and count.");

            Set("MockArchive",
                "Archive shaped rows served by the mock endpoints for demonstration and testing. Not real " +
                "traffic, and the mock endpoints are switched off in any environment being scanned or measured.");

            Set("ModelInvokeWarning",
                "One row per invocation that ran slower than its warning threshold, with the elapsed time, the " +
                "gap since the previous entry and the thread it ran on. Written only on breach.");

            Set("OpenTelemetryExclude",
                "Names excluded from OpenTelemetry collection, which is how a noisy instrument is silenced " +
                "without changing code.");

            Set("OpenTelemetryLogCounter",
                "A named regular expression counted against log output, which is how an arbitrary log pattern " +
                "becomes a metric.");

            Set("OpenTelemetryMetric",
                "Metrics received over OpenTelemetry, one row per instrument and tag set per sampling period, " +
                "with count, sum, minimum and maximum.");

            Set("OtlpDispatchCounter",
                "How the OpenTelemetry exporter itself is faring per signal: how many dispatches succeeded, " +
                "failed or were dropped, and how long they took. A rising DroppedCount means telemetry is being " +
                "lost in transit.");

            Set("OverlayNetworkTaskDrift",
                "Comparison of what DNS resolves a service to against what Swarm says its tasks are, which is how " +
                "a stale overlay network entry is caught before it causes intermittent failures.");

            Set("PatroniClusterEvent",
                "Changes observed in the Patroni cluster, such as a leader change, with the previous and new " +
                "value and the timeline it happened on.");

            Set("PatroniMemberStatus",
                "A sample of one Patroni member: its role and state, its timeline, its replication lag, and " +
                "whether it is waiting on a restart.");

            Set("PermissionSpecification",
                "The catalogue of permissions in the system, one row per permission. A role grants permissions by " +
                "referencing these through RoleRegistryPermission.");

            Set("PostgresLogEntry",
                "Postgres server log lines captured for the database observability pages.");

            Set("PostgresMetric",
                "A sample of the Postgres instance as a whole: connections, transaction and row throughput, cache " +
                "hit ratio, deadlocks, temporary file use, replication lag and database size. This is the " +
                "starting point for a database health question.");

            Set("PostgresReplicationStatus",
                "A sample of one replication connection: where the replica has got to, how far behind it is at " +
                "each stage, and whether it is synchronous.");

            Set("PreservationSnapshot",
                "A point in time export of a tenant's whole configuration, taken by hand from the Preservation " +
                "page or automatically before a change, and able to be restored. Restoring removes anything the " +
                "snapshot body does not hold, so every switch is exported whether or not it makes the body large. " +
                "SnapshotSourceId says what caused it to be taken.");

            Set("RedisCallCounter",
                "Timing of each kind of Redis call made by one instance per sampling period.");

            Set("RedisConnectionEvent",
                "Connection level Redis events, such as a failure or a restoration, with the endpoint, the retry " +
                "count and backoff, and how many transactions were affected.");

            Set("RedisConnectionMultiplexerMetric",
                "A detailed sample of one StackExchange.Redis multiplexer: outstanding work, completion counts " +
                "and socket state on both the interactive and subscription connections. This is where a Redis " +
                "timeout is diagnosed rather than merely observed.");

            Set("RedisMetric",
                "A sample of the Redis server as a whole: clients, memory against its limit, operations per " +
                "second, keyspace hits and misses, evictions and expiries, replication offset and persistence " +
                "state.");

            Set("RedisSentinelEvent",
                "Messages published by Redis Sentinel, which is where a failover announces itself.");

            Set("RedisSentinelStatus",
                "A sample of one entity known to Sentinel, whether master, replica or another sentinel, with its " +
                "link status, quorum and lag.");

            Set("RedisSlowOperation",
                "Entries from the Redis slow log, with the command, how long it took and which client issued it, " +
                "broken out by command and key name so a slow pattern can be grouped.");

            Set("RoleRegistry",
                "A role within a tenant. Users are assigned a role by UserRegistry.RoleRegistryGuid, the role is " +
                "granted permissions through RoleRegistryPermission, and it is given sight of individual case " +
                "workflow objects through the tables whose names end in Role.");

            Set("RoleRegistryPermission",
                "One permission granted to one role, referencing PermissionSpecification.");

            Set("RoleRegistryPermissionVersion",
                "Audit history for RoleRegistryPermission. On update the superseded RoleRegistryPermission row is " +
                "copied here before the live row is overwritten, so this holds previous states and never the " +
                "current one, and the live row carries a Version one ahead of the newest row here. " +
                "RoleRegistryPermissionId points back at the live row. See RoleRegistryPermission for what the " +
                "columns mean.");

            Set("RoleRegistryVersion",
                "Audit history for RoleRegistry. On update the superseded RoleRegistry row is copied here before " +
                "the live row is overwritten, so this holds previous states and never the current one, and the " +
                "live row carries a Version one ahead of the newest row here. RoleRegistryId points back at the " +
                "live row. See RoleRegistry for what the columns mean.");

            Set("RuleScriptToken",
                "Tokens recognised by the rule script language. Static platform data.");

            Set("SanctionEntry",
                "One sanction list entry, with the element value that is screened against, the reference it came " +
                "from and the payload behind it, hashed so a reimport can tell an unchanged entry from a new one.");

            Set("SanctionEntryImport",
                "One import of a sanction source, counting what was inserted, revived, left unchanged, removed " +
                "and rejected, with the error if it failed.");

            Set("SanctionEntryRejection",
                "One row a sanction import could not accept, with the raw data and the reason, so a bad source " +
                "file can be diagnosed without rerunning it.");

            Set("SanctionEntrySource",
                "A sanction list source: where it is fetched from, how it is delimited, and which columns carry " +
                "the screened string and the reference.");

            Set("SanctionStopToken",
                "Tokens ignored when matching sanction strings, such as corporate suffixes, grouped by category.");

            Set("SessionCaseJournal",
                "The case search a user last ran in a workflow, kept so the page can be returned to as it was " +
                "left.");

            Set("SessionCaseSearchCompiledSql",
                "A case search compiled from the builder's JSON into the SQL actually executed, split into its " +
                "select, where and order clauses and cached against the filter it came from. Rebuild marks one " +
                "that must be compiled again.");

            Set("SessionCaseSearchCompiledSqlExecution",
                "One execution of a compiled case search, with how long it took and how many records it returned. " +
                "This is where a slow case page is attributed to a particular filter.");

            Set("TenantRegistry",
                "A tenant. Almost every table in the system is scoped to one of these, directly or through a " +
                "chain of joins, and Landlord marks the tenant whose users administer the whole installation " +
                "rather than one tenancy.");

            Set("TenantRegistryVersion",
                "Audit history for TenantRegistry. On update the superseded TenantRegistry row is copied here " +
                "before the live row is overwritten, so this holds previous states and never the current one, and " +
                "the live row carries a Version one ahead of the newest row here. TenantRegistryId points back at " +
                "the live row. See TenantRegistry for what the columns mean.");

            Set("UserInTenant",
                "Which tenant a user is currently working in. This single row per user is what every tenant " +
                "scoped query in the system resolves the caller's tenancy through, so a user with no row here " +
                "sees nothing at all.");

            Set("UserInTenantSwitchLog",
                "The history of a user switching between tenants, which is the audit trail behind UserInTenant " +
                "holding only the current one.");

            Set("UserLogin",
                "One login attempt, successful or not, with the addresses and user agent it came from and why it " +
                "failed.");

            Set("UserLogout",
                "One session ending, with how it ended, whether it was cut by another user, and how long the " +
                "session had been open.");

            Set("UserRegistry",
                "A user account, with the hashed password and everything governing its expiry and lockout, and " +
                "the role it holds. TokensValidFrom invalidates every token issued before it, which is how a " +
                "session is revoked.");

            Set("UserRegistryApiKey",
                "An API key issued to a user, with the hashed key and a display fragment. The key itself is only " +
                "ever shown once, at creation.");

            Set("UserRegistryVersion",
                "Audit history for UserRegistry. On update the superseded UserRegistry row is copied here before " +
                "the live row is overwritten, so this holds previous states and never the current one, and the " +
                "live row carries a Version one ahead of the newest row here. UserRegistryId points back at the " +
                "live row. See UserRegistry for what the columns mean.");

            Set("VersionInfo",
                "The migrations FluentMigrator has applied to this database, one row per migration version. " +
                "Written by the migration runner at startup, never by the application.");

            Set("VisualisationRegistry",
                "A dashboard: the grid a set of datasources are laid out on, with its column count and cell " +
                "sizing.");

            Set("VisualisationRegistryDatasource",
                "One tile on a dashboard: the SQL it runs, how its result is rendered, and where it sits in the " +
                "grid. VisualisationTypeId says whether the display is a chart, a map or HTML.");

            Set("VisualisationRegistryDatasourceExecutionLog",
                "One execution of a dashboard tile's query, with how long it took and how many records it " +
                "returned.");

            Set("VisualisationRegistryDatasourceExecutionLogParameter",
                "The parameter values one dashboard tile execution was given.");

            Set("VisualisationRegistryDatasourceRole",
                "Grants one role sight of one VisualisationRegistryDatasource. This is an allow-list and not a " +
                "restriction list: a VisualisationRegistryDatasource with no row here is visible to nobody, " +
                "because every query that reads it requires a matching row for a role the caller holds. Joins " +
                "VisualisationRegistryDatasource by VisualisationRegistryDatasourceGuid and RoleRegistry by " +
                "RoleRegistryGuid, both on Guid rather than on Id, so that a configuration export carries the " +
                "grant with it.");

            Set("VisualisationRegistryDatasourceSeries",
                "A named, typed series a dashboard tile's query is expected to return, which is what the chart " +
                "binds to.");

            Set("VisualisationRegistryDatasourceVersion",
                "Audit history for VisualisationRegistryDatasource. On update the superseded " +
                "VisualisationRegistryDatasource row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. VisualisationRegistryDatasourceId points back at the live row. See " +
                "VisualisationRegistryDatasource for what the columns mean.");

            Set("VisualisationRegistryParameter",
                "A parameter a dashboard prompts for, with its datatype, default and whether it is required.");

            Set("VisualisationRegistryParameterRole",
                "Grants one role sight of one VisualisationRegistryParameter. This is an allow-list and not a " +
                "restriction list: a VisualisationRegistryParameter with no row here is visible to nobody, " +
                "because every query that reads it requires a matching row for a role the caller holds. Joins " +
                "VisualisationRegistryParameter by VisualisationRegistryParameterGuid and RoleRegistry by " +
                "RoleRegistryGuid, both on Guid rather than on Id, so that a configuration export carries the " +
                "grant with it.");

            Set("VisualisationRegistryParameterVersion",
                "Audit history for VisualisationRegistryParameter. On update the superseded " +
                "VisualisationRegistryParameter row is copied here before the live row is overwritten, so this " +
                "holds previous states and never the current one, and the live row carries a Version one ahead of " +
                "the newest row here. VisualisationRegistryParameterId points back at the live row. See " +
                "VisualisationRegistryParameter for what the columns mean.");

            Set("VisualisationRegistryRole",
                "Grants one role sight of one VisualisationRegistry. This is an allow-list and not a restriction " +
                "list: a VisualisationRegistry with no row here is visible to nobody, because every query that " +
                "reads it requires a matching row for a role the caller holds. Joins VisualisationRegistry by " +
                "VisualisationRegistryGuid and RoleRegistry by RoleRegistryGuid, both on Guid rather than on Id, " +
                "so that a configuration export carries the grant with it.");

            Set("VisualisationRegistryVersion",
                "Audit history for VisualisationRegistry. On update the superseded VisualisationRegistry row is " +
                "copied here before the live row is overwritten, so this holds previous states and never the " +
                "current one, and the live row carries a Version one ahead of the newest row here. " +
                "VisualisationRegistryId points back at the live row. See VisualisationRegistry for what the " +
                "columns mean.");

            Set("WafAttack",
                "One request the web application firewall matched against a signature, with where it came from, " +
                "what matched and what was done about it.");

            Set("WafException",
                "An exemption from a web application firewall signature for a particular route or field, which is " +
                "how a legitimate pattern is allowed through without disabling the signature.");

            Set("WafSignature",
                "One web application firewall signature: the pattern, what it scans, how long it may spend " +
                "matching, and whether a match drops the request or merely records it.");
        }

        public override void Down()
        {
        }

        private void Set(string table, string text)
        {
            var quoted = text.Replace("'", "''");

            Execute.Sql($"""
                         DO $$
                         BEGIN
                           IF EXISTS (SELECT 1 FROM information_schema.tables
                                      WHERE table_schema = 'public' AND table_name = '{table}') THEN
                             EXECUTE format('COMMENT ON TABLE public."{table}" IS %L', '{quoted}');
                           END IF;
                         END $$;
                         """);
        }
    }
}
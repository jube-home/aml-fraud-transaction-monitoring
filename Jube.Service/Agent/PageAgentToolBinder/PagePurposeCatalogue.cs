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

namespace Jube.Service.Agent.PageAgentToolBinder
{
    internal static class PagePurposeCatalogue
    {
        internal sealed record Entry(string Purpose, string LikelyAsks);

        internal static readonly IReadOnlyCollection<string> exemptPageNames =
        [
            "Copyright", "Error", "ChangePassword"
        ];

        private static readonly Dictionary<string, Entry> entries = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Index"] = new Entry(
                "The landing/home page -- an orientation point rather than a record of its own, typically " +
                "the first screen after signing in.",
                "where to find a particular feature or screen, and a general orientation to what Jube can " +
                "do -- there is no specific record in view here to ground an answer in, so lean on a broad " +
                "overview rather than guessing at a narrower intent."),

            ["Tree"] = new Entry(
                "The main navigation tree, used to drill from a detection model down into its rule " +
                "structures, or into other top-level areas of the platform.",
                "how to find or navigate to a specific model, rule, or area of the platform from here -- " +
                "there is no specific record in view yet, so treat this as a request for direction rather " +
                "than an answer about a particular record."),

            ["Case"] = new Entry(
                "A single case record under investigation: its status, assignment, notes, attachments, " +
                "transaction/event journal, activation-watcher alerts and sanctions screening. The record " +
                "in view carries this case's caseKey and caseKeyValue (plus entityAnalysisModelId and " +
                "caseWorkflowGuid) -- that is this specific case's identity, not a hint to go find: pass " +
                "those two values straight through as the key/value arguments to the case lookup tools " +
                "(the likes of CaseByCaseKeyValueGet, CaseNoteListByCaseKeyValue, CaseEventByCaseKeyValueGet, " +
                "CaseFileListByCaseKeyValue) rather than asking the caller which case they mean or searching " +
                "for one.",
                "what the case's current status and disposition are, why it was raised (which rule or " +
                "watcher triggered it), whether a note or attachment needs adding, what the " +
                "transaction/event journal or sanctions screening shows for this case, and what action or " +
                "escalation should happen next -- all of it anchored to the caseKey/caseKeyValue already in " +
                "the record above, never a different case silently assumed."),

            ["CaseSearch"] = new Entry(
                "Searching and filtering the tenant's cases by criteria such as status, assignment, model, " +
                "or date range, to find specific cases or get an overview of the caseload.",
                "how to narrow the search to a particular status, assignee, model, or date range, how many " +
                "cases match a given filter, and which specific case to open next."),

            ["EntityAnalysisModel"] = new Entry(
                "The detection model record itself: its name and activation state, and the entry point " +
                "into all the rule structures (abstraction, activation, gateway rules, counters, " +
                "sanctions, tags) built on it.",
                "whether the model is currently active, what rule structures it already has configured, " +
                "how to find or navigate to a specific rule, calculation, or counter under this model, and " +
                "what a proposed change to it would affect."),

            ["EntityAnalysisModelAbstractionRule"] = new Entry(
                "A single abstraction rule belonging to one detection model -- the search/aggregation " +
                "logic (search key, function, offset, interval) that derives a reusable value from " +
                "incoming data for that model's other rules to reference.",
                "what this rule currently computes and from which search key, function, and interval, " +
                "which model it belongs to, whether the rule or builder script validates cleanly, and what " +
                "would change if a parameter were adjusted."),

            ["Override"] = new Entry(
                "Looking up or managing override entries -- values (e.g. an account number or identifier) " +
                "force-flagged or force-allowed against a chosen override key, independent of normal " +
                "model scoring.",
                "which models support override on a given key, whether a specific value already has an " +
                "active override and what kind, and how to add, update, or remove an override entry for a " +
                "value."),

            ["RoleRegistry"] = new Entry(
                "The tenant's role registry: the named roles that bundle permissions and get assigned to " +
                "users, forming the basis of who can do what in Jube.",
                "what permissions a given role currently grants, which users hold a role, how to create or " +
                "adjust a role, and whether a role change could affect segregation of duties."),

            ["Cases"] = new Entry(
                "The Cases workspace: a tabbed view of the tenant's caseload with a ranked list, a My day view, queue health and bulk actions.",
                "what to look at first, how many cases match a filter, which cases are overdue or highest risk, and how to bulk assign or close a set."),

            ["Sanctions"] = new Entry(
                "Screening a name or entity against the loaded sanctions lists, showing ranked matches with their evidence.",
                "whether a name is a plausible match to a listed party, what distinguishes a true match from a coincidence, and how to document the outcome."),

            ["SanctionsLoader"] = new Entry(
                "Administration of the sanctions list loader: which lists are loaded, when they last refreshed and whether the last load succeeded.",
                "why a load failed or is stale, which lists are in force, and how many entries a list holds."),

            ["Svirfd"] = new Entry(
                "IP address intelligence lookups: geolocation, network ownership and anonymiser or hosting indicators for an address.",
                "whether an address looks risky (proxy, VPN, hosting, unusual country), who owns the network, and how that bears on a transaction."),

            ["Watcher"] = new Entry(
                "A read-only live view of activations as the engine raises them.",
                "what is firing right now, which rule or model is noisy, and whether a burst of activations is unusual."),

            ["Graph"] = new Entry(
                "A single network graph of entities linked by shared attributes, with analysis of clusters and central nodes.",
                "who is connected to whom, which node is most central, whether a cluster looks like a ring, and what to investigate next."),

            ["GraphDirectory"] = new Entry(
                "The directory of saved network graphs the user can open.",
                "which graph to open for a question, and what each graph covers."),

            ["Visualisation"] = new Entry(
                "A single saved visualisation (chart or dashboard) rendered from a governed data source.",
                "what the chart shows, whether a pattern is real, and how to build a variation of it."),

            ["VisualisationDirectory"] = new Entry(
                "The directory of saved visualisations the user can open.",
                "which visualisation answers a question and how to create a new one."),

            ["Performance"] = new Entry(
                "Operational performance counters for the engine: throughput, queue balance, response time pipelines and archiver or case creation stages.",
                "whether the platform is keeping up, where the bottleneck is, and whether a counter's trend is a developing problem."),

            ["Preservation"] = new Entry(
                "Preservation administration: snapshots and moving data between environments in encrypted packages.",
                "what a snapshot or package contains, whether a move is safe to run, and what the options mean."),

            ["Integrity"] = new Entry(
                "The integrity view of one detection model: checks that its rules, variables and configuration are internally consistent.",
                "what is inconsistent in the model, why a check failed, and how to fix it."),

            ["AdvancedAnalytics"] = new Entry(
                "The AdvancedAnalytics area: statistical and machine learning models built on the tenant's data, their instances, versions, training and approval.",
                "how a model performed, whether a new version beats the current one, what a variable contributes, and what to train or approve next."),

            ["models"] = new Entry(
                "The Models area: the detection models and every rule structure beneath them, edited as a story from model down to individual rule.",
                "how a model is configured, where to find a rule or counter, and what a proposed change would affect."),

            ["caseDetail"] = new Entry(
                "A single case record under investigation.",
                "the case's status, why it was raised, what the journal shows and what action to take next."),

            ["adminTenants"] = new Entry(
                "Tenant administration for the landlord: creating and managing tenants.",
                "which tenants exist, how one is configured, and what changing it affects."),

            ["adminSecurity"] = new Entry(
                "Security administration: users, roles, permissions, logins and logouts.",
                "who can do what, who logged in recently, and how to adjust a role or user safely."),

            ["adminErasure"] = new Entry(
                "Data erasure administration for privacy requests.",
                "what an erasure covers, whether a request is complete, and what is retained and why."),

            ["adminGovernance"] = new Entry(
                "Governance administration: approvals and the maker-checker process for changes.",
                "what is awaiting approval, who requested it and what the change would do."),

            ["adminQueue"] = new Entry(
                "Queue administration for the asynchronous processing queues.",
                "whether queues are backing up and what the depths mean."),

            ["adminReplay"] = new Entry(
                "Replay administration: re-processing historical transactions against a model.",
                "what a replay will do, its progress and whether it is safe to run."),

            ["adminResolution"] = new Entry(
                "Entity resolution administration: how records are matched into single entities.",
                "why two records were or were not merged and how to adjust the rules."),

            ["adminEntityRisk"] = new Entry(
                "Entity risk administration: scoring settings for entities over time.",
                "how entity risk is computed and which inputs move it."),

            ["adminPerformance"] = new Entry(
                "Operational performance counters for the engine.",
                "whether the platform is keeping up and where the bottleneck is."),

            ["adminVisualisations"] = new Entry(
                "Administration of the registry of visualisations.",
                "which visualisations are registered, who can see them and how to add one."),

            ["adminGraphs"] = new Entry(
                "Administration of the registry of network graphs.",
                "which graphs are registered and how to add or change one."),

            ["adminTelemetry"] = new Entry(
                "Telemetry administration: what the platform reports about its own operation.",
                "what is being collected and where to see it."),

            ["adminSanctionsLoader"] = new Entry(
                "Administration of the sanctions list loader.",
                "which lists are loaded and whether the last load succeeded."),

            ["adminPreservation"] = new Entry(
                "Preservation administration: snapshots and environment moves.",
                "what a snapshot or package contains and whether a move is safe."),

            ["graphView"] = new Entry(
                "A single network graph.",
                "who is connected to whom and which cluster to investigate."),

            ["visualisationView"] = new Entry(
                "A single saved visualisation.",
                "what the chart shows."),

            ["PostgresMetric"] = new Entry(
                "Live and recent Postgres health metrics for the platform's database -- connections, " +
                "throughput, replication and similar operational counters.",
                "whether current metrics look healthy or anomalous, what a specific metric's recent trend " +
                "has been, and whether there's a sign of a developing database problem worth escalating.")
        };

        internal static bool TryGet(string pageName, out Entry? entry)
        {
            return entries.TryGetValue(pageName, out entry);
        }

        internal static IReadOnlyCollection<string> KnownPageNames => entries.Keys;
    }
}
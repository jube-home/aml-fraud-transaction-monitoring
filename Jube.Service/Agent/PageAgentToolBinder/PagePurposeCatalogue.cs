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

        private static readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal)
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
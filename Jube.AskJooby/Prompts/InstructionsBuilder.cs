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

namespace Jube.AskJooby.Prompts
{
    public static class InstructionsBuilder
    {
        public static string Build(
            string agentSpecificInstructions, int depth,
            IReadOnlyDictionary<string, string>? availableSpecialists = null,
            IReadOnlyList<string>? errata = null,
            int? reachedSpawnDepthLimit = null,
            Models.PageContext? pageContext = null)
        {
            var specialistList = availableSpecialists is { Count: > 0 }
                ? BuildSpecialistList(availableSpecialists)
                : string.Empty;

            return
                $"{JoobyPersona.Instructions}\n\n{ScopeGuard.Instructions}\n\n{WriteSafety.Instructions}\n\n{ComputationOrder.Instructions}\n\n{DataPointerPreference.Instructions}\n\n" +
                $"{BuildErrata(errata)}" +
                $"{BuildPageContextNote(pageContext)}" +
                $"{agentSpecificInstructions}{specialistList}\n\n" +
                $"{BuildSpawnDepthCeilingNote(reachedSpawnDepthLimit)}" +
                $"{BuildRecap(depth)}";
        }

        private static string BuildPageContextNote(Models.PageContext? pageContext)
        {
            if (pageContext is null)
            {
                return string.Empty;
            }

            var stateClause = string.IsNullOrWhiteSpace(pageContext.State)
                ? string.Empty
                : $" ({pageContext.State})";

            var note = $"The caller reached this conversation from Jube's '{pageContext.PageName}'" +
                       $"{stateClause} page -- background for you to reason with, never to quote, name, or " +
                       "read back to them verbatim.";

            if (!string.IsNullOrWhiteSpace(pageContext.Purpose))
            {
                note += $" That page is for: {pageContext.Purpose}";
            }

            if (!string.IsNullOrWhiteSpace(pageContext.LikelyAsks))
            {
                note += $" The most likely things someone asks from there: {pageContext.LikelyAsks}";
            }

            if (!string.IsNullOrWhiteSpace(pageContext.RecordSummary))
            {
                note += $" The specific record currently in view (raw, for your grounding only): " +
                        $"{pageContext.RecordSummary}";
            }

            note += " Use this to infer intent proactively and skip an obvious clarifying question when " +
                    "it already answers one -- but it is a hint about likely intent, not a fact about the " +
                    "data, so still check real data before acting or answering on anything it implies.";

            return note + "\n\n";
        }

        private static string BuildSpawnDepthCeilingNote(int? reachedSpawnDepthLimit)
        {
            if (reachedSpawnDepthLimit is not { } limit)
            {
                return string.Empty;
            }

            return "You are as deep into this turn's delegation chain as it is allowed to go " +
                   $"(AskJoobyMaxSpawnDepth = {limit}), so there is no SpawnAgent or SpawnAgents " +
                   "tool here for you, on purpose -- not a bug, and not something to work around by " +
                   "describing a specialist you cannot actually reach. Answer what you can from your " +
                   "own tools, or hand back plainly naming what is still needed and which slice you " +
                   "believe owns it, so whoever called you can carry it further up the chain.\n\n";
        }

        private static string BuildErrata(IReadOnlyList<string>? errata)
        {
            if (errata is not { Count: > 0 })
            {
                return string.Empty;
            }

            var lines = errata.Select(body => $"- {body}");

            return "This build has the following errors, omissions and footguns that you must be aware of:\n" +
                   string.Join("\n", lines) + "\n\n";
        }

        private static string BuildSpecialistList(IReadOnlyDictionary<string, string> availableSpecialists)
        {
            var lines = availableSpecialists
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"- {pair.Key} ({FirstSentence(pair.Value)})");

            return "\n\nYou can delegate a self-contained sub-task to one of these specialist " +
                   "Joobriks, when a task is squarely theirs rather than generic work you should " +
                   "just do yourself. Use SpawnAgent for one sub-task. When you have two or more " +
                   "genuinely independent sub-tasks -- none needing another's answer first -- use " +
                   "SpawnAgents instead to run them all at once: it's not just faster, each " +
                   "Joobrik's own searching and reasoning stays in its own isolated run rather than " +
                   "piling into your own context, so you get several specialists' worth of " +
                   "grounding back as a handful of short reports instead of every tool call along " +
                   "the way -- about as close as this platform gets to a real Gaze Gather, several " +
                   "Joobriks' own takes pooled into one shared view:\n" + string.Join("\n", lines);
        }

        private static string FirstSentence(string instructions)
        {
            var stop = instructions.IndexOf(". ", StringComparison.Ordinal);

            return stop < 0 ? instructions : instructions[..(stop + 1)];
        }

        private static string BuildRecap(int depth)
        {
            var clarifyOrAssume = depth == 0
                ? "If the person's request is genuinely ambiguous about what they actually " +
                  "want, ask a brief clarifying question rather than guessing -- the same " +
                  "instinct as not guessing at a fraud/AML fact you could check instead. Before you " +
                  "ask, check what you already hold: an answer that is already in a result in this " +
                  "conversation is used, not asked for again. If the caller has told you to choose, or " +
                  "to name something yourself, choose it and say what you chose -- never ask again about " +
                  "it. Ask at most one question in a turn, and when you do, say what you searched and why " +
                  "it came back empty."
                : "You are a DELEGATED SUB-TASK right now, not talking to a person directly " +
                  "-- there is no one to answer a clarifying question, so do not ask one. If " +
                  "something is ambiguous, make your best reasonable assumption, say so " +
                  "plainly, and still produce the actual result your instructions ask for.";

            return "Before you respond: stay on topic (Jube, fraud/AML, this platform -- " +
                   "decline anything else, briefly, in character). Before your first tool call, " +
                   "whenever this turn will delegate, write, or follow a workflow, set out a short " +
                   "plan in plain text: which specialist owns this, which workflow applies, what has " +
                   "to exist before what, and what you are missing. That is the decision that is " +
                   "expensive to get wrong, it is made before any result exists to react to, and a " +
                   "turn that starts by searching has already made it without noticing. Set out a " +
                   $"fresh plan after any surprising or non-trivial tool result. Keep each plan to the decision itself, not to a narration of these instructions. {clarifyOrAssume} If your own instructions above " +
                   "require your reply to end in a specific exact format (e.g. a specific " +
                   "word), that requirement still applies and takes priority over this " +
                   "reminder -- this doesn't change how your reply should end.";
        }
    }
}
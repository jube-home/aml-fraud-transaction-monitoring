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

using System.Text.RegularExpressions;
using Jube.AskJooby.Validation.Models;

namespace Jube.AskJooby.Validation
{
    public static class ResponseValidator
    {
        private static readonly Regex statusPattern =
            new(@"^\s*STATUS:\s*(PASSED|FAILED)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex reasonPattern =
            new(@"REASON:\s*(.*?)(?:\r?\nKIND:|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex kindPattern =
            new(@"^\s*KIND:\s*(OMISSION|ERROR)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public static string BuildTask(
            string question, string docs, string reply, string toolEvidence = "", string systemContext = "")
        {
            var docsSection = string.IsNullOrWhiteSpace(docs)
                ? "Bites already found while answering: none -- the reply was written without a " +
                  "single Pasture search. Treat every claim in it as unconfirmed until you find a " +
                  "Bite that actually backs it up."
                : $"Bites already found while answering:\n[CRITICAL CONTEXT START]\n{docs}\n" +
                  "[CRITICAL CONTEXT END]\n\nThe Bites above are a starting point, not the full " +
                  "picture -- search the Pasture yourself for anything the reply states that isn't " +
                  "already backed by one of them. Each Bite is labelled with its own category (e.g. " +
                  "[Code/...] or [Document/...]) -- if a Code Bite and a Document Bite genuinely " +
                  "disagree, trust the code: it's what the software actually does, and docs can lag " +
                  "behind a change that's already shipped.";

            var toolEvidenceSection = string.IsNullOrWhiteSpace(toolEvidence)
                ? string.Empty
                : "\n\nTool calls actually executed this turn, before the reply was written, against " +
                  "the live platform itself:\n[CRITICAL CONTEXT START]\n" + toolEvidence +
                  "\n[CRITICAL CONTEXT END]\n\nTreat every one of these as already confirmed -- never " +
                  "search the Pasture for a number, row count, column name or other value that came " +
                  "from one of these calls, and never report it as an omission. Only fail a specific " +
                  "drawn from here when it actively contradicts what the call above returned.";

            var systemContextSection = string.IsNullOrWhiteSpace(systemContext)
                ? string.Empty
                : "\n\nStructured context the platform itself attached to this conversation -- the " +
                  "page the caller is on, its purpose, and the record behind it:\n[CRITICAL CONTEXT " +
                  "START]\n" + systemContext + "\n[CRITICAL CONTEXT END]\n\nThis came from the request " +
                  "itself, not from generation, and it was never going to be in the Pasture -- treat " +
                  "every field here as already confirmed, never search the Pasture for the page's " +
                  "identity, its purpose, or what it says about the record, and never report any of " +
                  "it as an omission. You can also call DescribeSystemContext yourself for the same " +
                  "information.";

            return "Check the reply for invented specifics. A specific is a name, a tool or operation " +
                   "name, a config key, a number, or a piece of syntax. Fail the reply only when one " +
                   "specific is unsupported by the Bites or contradicts them. Paraphrase, summary and " +
                   "synthesis across Bites are correct and are not failures. Ignore style, formatting " +
                   "and completeness. Do not rewrite the text.\n\n" +
                   "Skip this check entirely for a specific that names a live record of the running " +
                   "platform -- a particular model, rule, counter or conversation's own id, guid, " +
                   "name, field value, count, or current state -- or that describes the page the " +
                   "caller is on, that page's purpose, or what the platform says the record on it " +
                   "contains. None of that was ever going to be in the Pasture: a live value is " +
                   "documentation and code, never a live copy of the database, and a page's identity " +
                   "and purpose is structured context the platform attaches to the request itself, " +
                   "not generated text drawn from docs. A search turning up nothing for either is not " +
                   "an omission, it is the wrong place to look. That reply was already checked the " +
                   "only way it could be -- a live value by a tool call against the platform itself, " +
                   "a page claim by the structured context attached to this conversation -- before " +
                   "you ever saw it. See the tool-call evidence and system context below when they " +
                   "are given, and when they are not, take the same claim on trust rather than " +
                   "treating the Pasture's silence as a gap. Keep checking everything else as normal: " +
                   "tool and operation names, config keys, syntax, and how something works in " +
                   "general.\n\n" +
                   "A person is waiting on this check before they see their reply, so work " +
                   "economically: most replies settle within two or three searches total. Rewording " +
                   "a query you already tried is not a new specific -- it is the same question again " +
                   "-- so if you are well past three searches and still have not turned up anything " +
                   "new, stop there rather than trying yet another phrasing: that itself is the " +
                   "answer, an OMISSION, not a reason to keep searching.\n\n" +
                   "Output your response strictly in this format and nothing else:\n" +
                   "STATUS: [PASSED or FAILED]\n" +
                   "REASON: [If FAILED, one sentence quoting the unsupported or incorrect specific. If " +
                   "PASSED, leave blank.]\n" +
                   "KIND: [Only if FAILED -- OMISSION if you searched and found nothing that confirms or " +
                   "contradicts it; ERROR if a Bite contradicts it, or the answer is in the Bites and the " +
                   "reply got it wrong. When in doubt, ERROR.]\n\n" +
                   "For an OMISSION, say only that the claim could not be CONFIRMED from the Pasture. " +
                   "Never call it invented, fabricated or wrong unless a Bite contradicts it.\n\n" +
                   $"Question asked: {question}\n\nGenerated reply to check:\n{reply}\n\n{docsSection}" +
                   $"{toolEvidenceSection}{systemContextSection}\n\n" +
                   "Search only for specifics you cannot find in the Bites, and never twice for the same " +
                   "one. Two empty searches on one specific is an OMISSION. " +
                   "Now answer with STATUS, REASON and KIND, and nothing else.";
        }

        public static ValidationResult ParseResult(string modelOutput)
        {
            var text = modelOutput.Trim();
            var statusMatch = statusPattern.Match(text);

            if (statusMatch.Success &&
                statusMatch.Groups[1].Value.Equals("PASSED", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(true, null);
            }

            var reasonMatch = reasonPattern.Match(text);
            var feedback = reasonMatch.Success ? reasonMatch.Groups[1].Value.Trim() : text;

            var kindMatch = kindPattern.Match(text);

            var failureKind = kindMatch.Success &&
                              kindMatch.Groups[1].Value.Equals("OMISSION", StringComparison.OrdinalIgnoreCase)
                ? ValidationFailureKind.Omission
                : ValidationFailureKind.Error;

            return new ValidationResult(false, string.IsNullOrWhiteSpace(feedback) ? text : feedback, failureKind);
        }
    }
}
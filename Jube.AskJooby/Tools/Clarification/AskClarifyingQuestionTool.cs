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

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jube.AskJooby.Tools.Clarification
{
    public sealed record AskClarifyingQuestionPayload(
        [property: JsonPropertyName("question")]
        string Question,
        [property: JsonPropertyName("options")]
        IReadOnlyList<string> Options,
        [property: JsonPropertyName("allowFreeText")]
        bool AllowFreeText,
        [property: JsonPropertyName("instruction")]
        string Instruction);

    public static class AskClarifyingQuestionTool
    {
        [Description(
            "Call this INSTEAD OF writing a prose reply when the caller's request is genuinely " +
            "ambiguous between a small number of distinct, non-overlapping readings -- for example " +
            "\"how many rules do I have\" when Jube has several unrelated rule types. Presents the " +
            "question and a fixed set of options to the caller as a selectable prompt (checkboxes, " +
            "not free text) so they can pick one, several, or type their own answer instead. This " +
            "call IS your entire reply for this turn -- after calling it, write nothing further and " +
            "call no other tool; wait for the caller's next message. Only list options you can " +
            "actually stand behind -- an invented option here is exactly as wrong as one stated in " +
            "prose, and gets checked the same way.")]
        public static string AskClarifyingQuestion(
            [Description("The question to ask, one sentence, no leading filler.")]
            string question,
            [Description("2-6 short option labels the caller can pick from (each a few words, no " +
                         "numbering/bullets -- the UI adds those). The caller can tick several at " +
                         "once, so phrase each label as something independently actionable on its " +
                         "own, never as mutually exclusive alternatives (avoid wording like \"X " +
                         "only\" or \"not Y\") -- if the caller ticks two, you must be able to act on " +
                         "both together, not treat them as contradicting each other. Every option " +
                         "must be something you could actually act on if chosen.")]
            List<string>? options,
            [Description("Whether the caller may also just type a free-text answer instead of " +
                         "picking an option. True unless the options are genuinely exhaustive.")]
            bool allowFreeText = true)
        {
            var payload = new AskClarifyingQuestionPayload(
                question, options ?? [], allowFreeText,
                "Already shown to the caller as a selectable prompt -- do not repeat the question or " +
                "options in your own reply text, and do not call any further tools this turn. Your " +
                "turn is complete; wait for their answer.");

            return JsonSerializer.Serialize(payload);
        }
    }
}
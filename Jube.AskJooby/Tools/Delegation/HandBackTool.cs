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
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Delegation.Models;
using Jube.AskJooby.Tools.Delegation.Interfaces;

namespace Jube.AskJooby.Tools.Delegation
{
    public sealed class HandBackTool(IHandBackSignal signal)
    {
        [Description(
            "Hands this sub-task back to whoever sent you, because the work belongs to a slice you do " +
            "not hold. Call this the moment you establish that the answer is not in your own " +
            "operations -- not after writing SQL against another slice's tables, and not after " +
            "reporting that you could not find it. Whoever sent you can reach specialists you cannot, " +
            "so handing back is how the question gets to the one that owns it. Say what you did " +
            "establish, because that is kept and not asked for again -- if that includes several " +
            "numeric or tabular facts the caller might chart or export, cache them with " +
            "CacheAssembledTable first and name the data_pointer here rather than listing bare " +
            "numbers in prose. This ends your sub-task: write nothing further after calling it.")]
        public string HandBack(
            [Description(
                "What is actually needed, in one sentence, phrased as the question the next specialist " +
                "has to answer -- not a summary of your own difficulty. Say 'Not stated.' where you " +
                "genuinely have no specific question to hand onward.")]
            string whatIsNeeded = "Not stated.",
            [Description(
                "What you did establish before stopping, so it is not worked out twice. Say 'nothing' " +
                "where that is the truth.")]
            string whatIEstablished = "nothing",
            [Description(
                "The agent or slice you believe owns it, named only where you have actually seen that " +
                "name in the Pasture or in your own instructions. Leave empty rather than guessing: an " +
                "unnamed hand back still reaches someone who can route it, a wrong name sends it " +
                "somewhere that will hand it back again, and a name that does not exist at all is worse " +
                "still -- it is checked against the real roster, and dropped if it is not on it.")]
            string? owningAgent = null)
        {
            var trimmedOwningAgent = string.IsNullOrWhiteSpace(owningAgent) ? null : owningAgent.Trim();

            var unknownOwningAgentNotice = trimmedOwningAgent is not null
                                           && !AgentDefinitions.All.ContainsKey(trimmedOwningAgent)
                ? $" No agent named '{trimmedOwningAgent}' is registered, so that claim has been dropped " +
                  "rather than passed on -- naming a real agent only helps if it is one, and a plausible-" +
                  "looking guess is worse than leaving this unnamed."
                : "";

            signal.Raise(new HandBack(
                trimmedOwningAgent is not null && AgentDefinitions.All.ContainsKey(trimmedOwningAgent)
                    ? trimmedOwningAgent
                    : null,
                whatIsNeeded,
                whatIEstablished));

            return "Handed back. Stop here -- do not keep working on it and do not write a report. "
                   + "Whoever sent you has been told what is needed and what you established."
                   + unknownOwningAgentNotice;
        }
    }
}
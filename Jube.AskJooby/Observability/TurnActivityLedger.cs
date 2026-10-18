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

using System.Collections.Concurrent;
using System.Globalization;
using Jube.AskJooby.Observability.Interfaces;

namespace Jube.AskJooby.Observability
{
    public sealed class TurnActivityLedger(ITokenBudget? budget = null) : ITurnActivityLedger
    {
        private int agentCalls;
        private int handBacks;
        private int toolCalls;
        private int unrequestedReasoning;
        private int unplannedHandovers;
        private string? plan;
        private HashSet<string>? plannedStables;
        private readonly ConcurrentDictionary<string, string> stablePlans = new(StringComparer.OrdinalIgnoreCase);
        private static readonly IReadOnlySet<string> emptyStables = new HashSet<string>();
        public int AgentCalls => Volatile.Read(ref agentCalls);
        public int ToolCalls => Volatile.Read(ref toolCalls);

        public void RecordAgentCall()
        {
            Interlocked.Increment(ref agentCalls);
        }

        public int HandBacks => Volatile.Read(ref handBacks);

        public void NoteHandBack()
        {
            Interlocked.Increment(ref handBacks);
        }

        public void NoteUnrequestedReasoning()
        {
            Interlocked.Increment(ref unrequestedReasoning);
        }

        public int UnrequestedReasoningUpdates => Volatile.Read(ref unrequestedReasoning);
        public bool HasPlan => Volatile.Read(ref plan) is not null;
        public IReadOnlySet<string> PlannedStables => Volatile.Read(ref plannedStables) ?? emptyStables;

        public bool HasStablePlan(string stable)
        {
            return stablePlans.ContainsKey(stable);
        }

        public void RecordStablePlan(string stable, string stablePlan)
        {
            stablePlans[stable] = stablePlan;
        }

        public void RecordPlan(string newPlan, IReadOnlySet<string> stables)
        {
            Volatile.Write(ref plannedStables, new HashSet<string>(stables, StringComparer.OrdinalIgnoreCase));
            Volatile.Write(ref plan, newPlan);
        }

        public int UnplannedHandovers => Volatile.Read(ref unplannedHandovers);

        public void NoteUnplannedHandover()
        {
            Interlocked.Increment(ref unplannedHandovers);
        }

        public void RecordToolCall()
        {
            Interlocked.Increment(ref toolCalls);
        }

        public string Describe()
        {
            var spentSoFar = budget?.Spent ?? 0;

            var cost = spentSoFar > 0
                ? $", {spentSoFar.ToString("N0", CultureInfo.InvariantCulture)} tokens spent"
                : string.Empty;

            var thoughtAnyway = UnrequestedReasoningUpdates > 0
                ? $" Thinking is switched off and the model reasoned anyway on "
                  + $"{UnrequestedReasoningUpdates.ToString(CultureInfo.InvariantCulture)} update(s), so the "
                  + "first token is being waited for and paid for regardless."
                : string.Empty;

            return $"[Turn so far: {AgentCalls.ToString(CultureInfo.InvariantCulture)} Joobrik(s) sent out, "
                   + $"{ToolCalls.ToString(CultureInfo.InvariantCulture)} tool call(s){cost}.{thoughtAnyway}]";
        }
    }
}
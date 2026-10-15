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

namespace Jube.AskJooby.Observability.Interfaces
{
    public interface ITurnActivityLedger
    {
        int HandBacks { get; }
        int UnrequestedReasoningUpdates { get; }
        int AgentCalls { get; }
        int ToolCalls { get; }
        void RecordAgentCall();
        void NoteHandBack();
        void RecordToolCall();
        void NoteUnrequestedReasoning();
        bool HasPlan { get; }
        IReadOnlySet<string> PlannedStables { get; }
        int UnplannedHandovers { get; }
        void RecordPlan(string plan, IReadOnlySet<string> stables);
        bool HasStablePlan(string stable);
        void RecordStablePlan(string stable, string plan);
        void NoteUnplannedHandover();
        string Describe();
    }
}
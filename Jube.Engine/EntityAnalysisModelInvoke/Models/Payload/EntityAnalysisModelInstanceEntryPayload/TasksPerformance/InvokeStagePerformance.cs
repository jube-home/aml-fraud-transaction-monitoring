/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;

namespace Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance
{
    public class InvokeStagePerformance
    {
        public StageDuration Parse { get; set; }
        public StageDuration CheckIntegrityAndUpsert { get; set; }
        public StageTiming<TaskPerformance> InlineFunctions { get; set; }
        public StageTiming<TaskPerformance> InlineScripts { get; set; }
        public StageTiming<TaskPerformance> Gateway { get; set; }
        public StageDuration CacheDbStorage { get; set; }
        public StageTiming<TaskPerformance> Sanctions { get; set; }
        public StageTiming<TaskPerformance> TtlCounters { get; set; }
        public StageTiming<TaskPerformance> AbstractionRulesWithSearchKeys { get; set; }
        public StageDuration JoinReadTasks { get; set; }
        public StageTiming<TaskPerformance> AbstractionRulesWithoutSearchKeys { get; set; }
        public StageTiming<TaskPerformance> AbstractionCalculations { get; set; }
        public StageTiming<TaskPerformance> ExhaustiveAdaptation { get; set; }
        public StageTiming<TaskPerformance> HttpAdaptation { get; set; }
        public StageTiming<ActivationRuleTiming> Activation { get; set; }
        public StageDuration JoinWriteTasks { get; set; }
        public StageDuration WriteResponse { get; set; }
        public StageDuration BuildArchivePayload { get; set; }

        public IEnumerable<(string Name, StageDuration Stage)> EnumerateStages()
        {
            if (Parse != null)
            {
                yield return (nameof(Parse), Parse);
            }

            if (CheckIntegrityAndUpsert != null)
            {
                yield return (nameof(CheckIntegrityAndUpsert), CheckIntegrityAndUpsert);
            }

            if (InlineFunctions != null)
            {
                yield return (nameof(InlineFunctions), InlineFunctions);
            }

            if (InlineScripts != null)
            {
                yield return (nameof(InlineScripts), InlineScripts);
            }

            if (Gateway != null)
            {
                yield return (nameof(Gateway), Gateway);
            }

            if (CacheDbStorage != null)
            {
                yield return (nameof(CacheDbStorage), CacheDbStorage);
            }

            if (Sanctions != null)
            {
                yield return (nameof(Sanctions), Sanctions);
            }

            if (TtlCounters != null)
            {
                yield return (nameof(TtlCounters), TtlCounters);
            }

            if (AbstractionRulesWithSearchKeys != null)
            {
                yield return (nameof(AbstractionRulesWithSearchKeys), AbstractionRulesWithSearchKeys);
            }

            if (JoinReadTasks != null)
            {
                yield return (nameof(JoinReadTasks), JoinReadTasks);
            }

            if (AbstractionRulesWithoutSearchKeys != null)
            {
                yield return (nameof(AbstractionRulesWithoutSearchKeys), AbstractionRulesWithoutSearchKeys);
            }

            if (AbstractionCalculations != null)
            {
                yield return (nameof(AbstractionCalculations), AbstractionCalculations);
            }

            if (ExhaustiveAdaptation != null)
            {
                yield return (nameof(ExhaustiveAdaptation), ExhaustiveAdaptation);
            }

            if (HttpAdaptation != null)
            {
                yield return (nameof(HttpAdaptation), HttpAdaptation);
            }

            if (Activation != null)
            {
                yield return (nameof(Activation), Activation);
            }

            if (JoinWriteTasks != null)
            {
                yield return (nameof(JoinWriteTasks), JoinWriteTasks);
            }

            if (WriteResponse != null)
            {
                yield return (nameof(WriteResponse), WriteResponse);
            }

            if (BuildArchivePayload != null)
            {
                yield return (nameof(BuildArchivePayload), BuildArchivePayload);
            }
        }
    }
}
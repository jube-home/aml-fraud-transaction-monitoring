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
    public class TaskWrapperStats
    {
        public ReadTasksPerformance Read { get; set; }
        public WriteTasksPerformance Write { get; set; }

        public IEnumerable<(string Direction, string Name, TaskPerformance Task)> EnumerateTasks()
        {
            if (Read != null)
            {
                if (Read.SanctionsAsync != null)
                {
                    yield return ("Read", nameof(Read.SanctionsAsync), Read.SanctionsAsync);
                }

                if (Read.TtlCountersAsync != null)
                {
                    yield return ("Read", nameof(Read.TtlCountersAsync), Read.TtlCountersAsync);
                }

                if (Read.AbstractionRulesWithSearchKeysAsync != null)
                {
                    yield return ("Read", nameof(Read.AbstractionRulesWithSearchKeysAsync),
                        Read.AbstractionRulesWithSearchKeysAsync);
                }
            }

            if (Write != null)
            {
                if (Write.CachePayloadLatestUpsertAsync != null)
                {
                    yield return ("Write", nameof(Write.CachePayloadLatestUpsertAsync),
                        Write.CachePayloadLatestUpsertAsync);
                }

                if (Write.CachePayloadUpsertAsync != null)
                {
                    yield return ("Write", nameof(Write.CachePayloadUpsertAsync), Write.CachePayloadUpsertAsync);
                }

                if (Write.CachePayloadInsertAsync != null)
                {
                    yield return ("Write", nameof(Write.CachePayloadInsertAsync), Write.CachePayloadInsertAsync);
                }

                if (Write.CacheTtlCounterEntryUpsertAsync != null)
                {
                    yield return ("Write", nameof(Write.CacheTtlCounterEntryUpsertAsync),
                        Write.CacheTtlCounterEntryUpsertAsync);
                }

                if (Write.CacheTtlCounterEntryIncrementAsync != null)
                {
                    yield return ("Write", nameof(Write.CacheTtlCounterEntryIncrementAsync),
                        Write.CacheTtlCounterEntryIncrementAsync);
                }

                if (Write.CacheSanctionInsertAsync != null)
                {
                    yield return ("Write", nameof(Write.CacheSanctionInsertAsync), Write.CacheSanctionInsertAsync);
                }

                if (Write.UpsertReferenceDateAsync != null)
                {
                    yield return ("Write", nameof(Write.UpsertReferenceDateAsync), Write.UpsertReferenceDateAsync);
                }
            }
        }
    }
}
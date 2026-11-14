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

using System;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using log4net;

namespace Jube.Engine.CaseEscalation
{
    public static class CaseChecklistSlaSweepSignal
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CaseChecklistSlaSweepSignal));

        public static async Task<int> RunAsync(DbContext dbContext, int batchSize, CancellationToken token = default)
        {
            var sweep = CaseChecklistSlaSweepRegistry.Current;

            if (sweep == null)
            {
                return 0;
            }

            try
            {
                return await sweep.SweepAsync(dbContext, batchSize, token).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.Error($"Case checklist SLA sweep: the pass could not be completed: {e}");

                return 0;
            }
        }
    }
}

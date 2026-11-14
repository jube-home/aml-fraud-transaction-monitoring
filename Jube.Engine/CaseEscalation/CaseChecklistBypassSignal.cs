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
    public static class CaseChecklistBypassSignal
    {
        public const string EscalationActor = "Case escalation";
        public const string ExpiryActor = "Case expiry";

        private static readonly ILog log = LogManager.GetLogger(typeof(CaseChecklistBypassSignal));

        public static async Task RecordAsync(DbContext dbContext, int caseId, string actor,
            CancellationToken token = default)
        {
            var bypass = CaseChecklistBypassRegistry.Current;

            if (bypass == null)
            {
                return;
            }

            try
            {
                await bypass.RecordAsync(dbContext, caseId, actor, token).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.Error($"Case checklist bypass: case {caseId} by {actor} could not be recorded: {e}");
            }
        }
    }
}

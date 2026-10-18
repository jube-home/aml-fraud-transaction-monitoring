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

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jube.AskJooby.Observability.Models;
using Jube.AskJooby.Routing.Interfaces;
using log4net;

namespace Jube.App.Code.AskJooby
{
    public sealed class LoggingAgentRoutingLedger(ILog log) : IAgentRoutingLedger
    {
        public Task RecordAsync(AgentRoutingDecision decision, CancellationToken token = default)
        {
            var embeddings = decision.EmbeddingRequests == 0
                ? "no embedding call"
                : $"{decision.EmbeddingRequests.ToString(CultureInfo.InvariantCulture)} embedding request(s) "
                  + $"covering {decision.EmbeddingValues.ToString(CultureInfo.InvariantCulture)} value(s)";

            var confidence = decision.Score is null
                ? string.Empty
                : $", best similarity {decision.Score.Value.ToString("F3", CultureInfo.InvariantCulture)}";

            var text = $"AskJooby routing: {decision.Stage} chose {decision.AgentType} in "
                       + $"{decision.DurationMs.ToString(CultureInfo.InvariantCulture)}ms with {embeddings}"
                       + $"{confidence}.";

            if (decision.Error is not null)
            {
                log.Warn($"{text} The embedding tier failed and was skipped, so this turn paid for the "
                         + $"model routing tier instead. The failure was: {decision.Error}");
            }
            else if (log.IsInfoEnabled)
            {
                log.Info(text);
            }

            return Task.CompletedTask;
        }
    }
}
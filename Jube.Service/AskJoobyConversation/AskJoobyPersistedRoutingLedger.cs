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

using Jube.AskJooby.Observability.Models;
using Jube.AskJooby.Routing.Interfaces;
using Jube.Data.Poco;
using Jube.Data.Repository;

namespace Jube.Service.AskJoobyConversation
{
    internal sealed class AskJoobyPersistedRoutingLedger(
        AskJoobyRoutingRepository repository,
        int? askJoobyConversationId,
        int tenantRegistryId,
        string userName,
        string task) : IAgentRoutingLedger
    {
        public int? RoutingId { get; private set; }
        public AgentRoutingDecision? Decision { get; private set; }

        public async Task RecordAsync(AgentRoutingDecision decision, CancellationToken token = default)
        {
            var row = await repository.InsertAsync(new AskJoobyRouting
            {
                AskJoobyConversationId = askJoobyConversationId,
                TenantRegistryId = tenantRegistryId,
                Task = task,
                Stage = (int)decision.Stage,
                AgentType = decision.AgentType,
                Score = decision.Score,
                EmbeddingRequests = decision.EmbeddingRequests,
                EmbeddingValues = decision.EmbeddingValues,
                DurationMs = decision.DurationMs,
                Error = decision.Error,
                CreatedUser = userName
            }, token).ConfigureAwait(false);

            RoutingId = row.Id;
            Decision = decision;
        }
    }
}
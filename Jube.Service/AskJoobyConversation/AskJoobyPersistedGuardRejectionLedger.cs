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

using Jube.AskJooby.Observability;
using Jube.AskJooby.Security.Interfaces;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;

namespace Jube.Service.AskJoobyConversation
{
    internal sealed class AskJoobyPersistedGuardRejectionLedger(
        AskJoobyAgentGuardRejectionRepository repository,
        int? askJoobyConversationId,
        int tenantRegistryId,
        string userName) : IAgentGuardRejectionLedger
    {
        public Task RecordAsync(AgentGuardRejection rejection, CancellationToken token = default)
        {
            return repository.InsertAsync(new AskJoobyAgentGuardRejection
            {
                AskJoobyConversationId = askJoobyConversationId,
                TenantRegistryId = tenantRegistryId,
                PhaseId = MapPhase(rejection.Phase),
                PipelineName = rejection.PipelineName,
                RuleName = rejection.RuleName,
                Reason = rejection.Reason,
                ActionId = MapAction(rejection.Action),
                CreatedUser = userName
            }, token);
        }

        private static AskJoobyGuardPhase? MapPhase(string? phase)
        {
            return Enum.TryParse<AskJoobyGuardPhase>(phase, true, out var parsed) ? parsed : null;
        }

        private static AskJoobyGuardAction? MapAction(string? action)
        {
            return Enum.TryParse<AskJoobyGuardAction>(action, true, out var parsed) ? parsed : null;
        }
    }
}
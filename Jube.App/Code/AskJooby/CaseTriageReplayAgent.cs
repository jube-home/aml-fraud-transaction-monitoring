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
using Jube.Service.Query.CaseNarrative.Models;
using Jube.Service.Query.CaseTriage.Interfaces;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Jube.App.Code.AskJooby
{
    public sealed class CaseTriageReplayAgent(IServiceProvider serviceProvider, ILog log) : ICaseTriageReplayAgent
    {
        public async Task<CaseNarrativeAnswer> AskAsync(string prompt, string facts, CancellationToken token = default)
        {
            var chatClient = serviceProvider.GetService<IChatClient>();

            if (chatClient is null)
            {
                log.Warn("CaseTriageReplayAgent: AskJooby is not configured on this node; nothing was asked.");

                return null;
            }

            var response = await chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.System, prompt), new ChatMessage(ChatRole.User, facts)],
                new ChatOptions { Temperature = 0 }, token).ConfigureAwait(false);

            return new CaseNarrativeAnswer(response.Text ?? string.Empty, null,
                response.ModelId ?? chatClient.GetService<ChatClientMetadata>()?.DefaultModelId);
        }
    }
}

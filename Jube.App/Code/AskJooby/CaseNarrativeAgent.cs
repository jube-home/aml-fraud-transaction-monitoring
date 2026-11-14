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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Service.Query.CaseNarrative.Interfaces;
using Jube.Service.Query.CaseNarrative.Models;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Jube.App.Code.AskJooby
{
    public sealed class CaseNarrativeAgent(
        IServiceProvider serviceProvider,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        IStringLocalizerFactory localizers,
        IServiceChangeBus serviceChangeBus) : ICaseNarrativeAgent
    {
        public async Task<CaseNarrativeAnswer> AskAsync(string userName, string prompt, string pageContext,
            CancellationToken token = default)
        {
            var chatClient = serviceProvider.GetService<IChatClient>();
            var validationChatClient = serviceProvider.GetKeyedService<IChatClient>("validation");
            var embeddingGenerator = serviceProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();

            if (chatClient is null || validationChatClient is null || embeddingGenerator is null)
            {
                log.Warn("CaseNarrativeAgent: AskJooby is not configured on this node; nothing was asked.");

                return null;
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);

            var service = await AskJoobyConversationServiceFactory.CreateAsync(dbContext, userName,
                dynamicEnvironment, log, localizers, serviceChangeBus, chatClient, validationChatClient,
                embeddingGenerator, serviceProvider, token, true).ConfigureAwait(false);

            var modelName = chatClient.GetService<ChatClientMetadata>()?.DefaultModelId;
            var conversation = await service.CreateConversationAsync(token).ConfigureAwait(false);
            var streamed = new StringBuilder();

            await foreach (var item in service.SendMessageAsync(conversation.Id, prompt, pageContext, null, token)
                               .ConfigureAwait(false))
            {
                switch (item.Kind)
                {
                    case "Text":
                        streamed.Append(item.Text);

                        break;
                    case "Guard" or "Error":
                        log.Warn($"CaseNarrativeAgent: AskJooby stopped for user '{userName}': {item.Kind}.");

                        return new CaseNarrativeAnswer(string.Empty, conversation.Id, modelName);
                }
            }

            var messages = await service.GetMessagesAsync(conversation.Id, token).ConfigureAwait(false);

            var reply = messages.LastOrDefault(w => w.Kind == "Message" && w.Role == "Assistant")?.Text;

            return new CaseNarrativeAnswer(string.IsNullOrWhiteSpace(reply) ? streamed.ToString() : reply,
                conversation.Id, modelName);
        }
    }
}

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

using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Agents.Interfaces;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Tools.Export.Interfaces;
using Jube.AskJooby.Tools.History.Interfaces;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.AskJooby.Tools.ScriptExecution.Interfaces;
using Jube.AskJooby.Tools.SqlQuery.Interfaces;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Agents
{
    public static class RunnableFactory
    {
        public static async Task<IRunnableAgent> CreateAsync(
            string type, IChatClient chatClient, IRagClient rag, IMemoryClient memory,
            IConversationHistoryClient history, string task, int depth, RunContext context,
            ISqlQueryClient? sqlQuery = null, IToolResultCache? cache = null, IFileExportClient? export = null,
            IScriptExecutionClient? scriptExecution = null, CancellationToken token = default)
        {
            var (agent, runId) = await AgentFactory
                .CreateAsync(type, chatClient, rag, memory, history, task, depth, context, sqlQuery, cache, export,
                    scriptExecution, token)
                .ConfigureAwait(false);

            return new BasicAgentRunnable(agent, runId, context);
        }
    }
}
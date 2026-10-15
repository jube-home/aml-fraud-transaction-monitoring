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

using Jube.AskJooby.Agents.Basic.Models;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.PageAgentToolBinder.Models;
using log4net;
using Microsoft.Extensions.AI;

namespace Jube.Service.Agent.PageAgentToolBinder
{
    public static class PageAgentToolBinder
    {
        internal static void AddBound(
            ServiceAgentBindingContext ctx, List<AgentToolDefinition> tools, string toolName, AIFunction function)
        {
            var descriptor = ServiceToolCatalogue.ServiceToolCatalogue.All.Single(d => d.Name == toolName);
            var requiresSnapshot = descriptor.Kind is OperationKind.Write or OperationKind.Delete;

            if (ctx.ReadOnly && requiresSnapshot)
            {
                return;
            }

            tools.Add(new AgentToolDefinition(function, descriptor.RequiredPermissions,
                requiresSnapshot, descriptor.RequiresLandlord));
        }

        public static IReadOnlyList<AgentToolDefinition> Bind(
            ServiceAgentBindingContext context, IReadOnlyList<string> toolNames, ILog log)
        {
            var cache = new ServiceInstanceCache(context, log);
            var tools = new List<AgentToolDefinition>();

            foreach (var toolName in toolNames)
            {
                var binding = ServiceOperationIndex.Get(toolName);

                if (!ServiceInstanceCache.CanConstruct(binding.ServiceType, context.Services))
                {
                    continue;
                }

                var function = AIFunctionFactory.Create(binding.Method,
                    _ => cache.Get(binding.ServiceType),
                    new AIFunctionFactoryOptions { Name = toolName, ExcludeResultSchema = true });

                AddBound(context, tools, toolName,
                    new ServiceBoundAiFunction(function, binding.ServiceType, cache,
                        context.ResultCache, context.MaxToolResultCharacters));
            }

            return tools;
        }
    }
}
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

using System.ComponentModel;
using System.Reflection;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Microsoft.Extensions.AI;

namespace Jube.Service.Agent.RuleExtensionTools
{
    public static class RuleExtensionToolBinder
    {
        private static readonly RuleExtensionTools instance = new();

        private static readonly Lazy<IReadOnlyDictionary<string, MethodInfo>> methods = new(() =>
            typeof(RuleExtensionTools)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .ToDictionary(method => method.Name, method => method, StringComparer.Ordinal));

        public static IReadOnlyList<AgentToolDefinition> Bind(IReadOnlyList<string> toolNames,
            IToolResultCache? resultCache = null, int maxToolResultCharacters = 6000)
        {
            var tools = new List<AgentToolDefinition>();

            foreach (var toolName in toolNames)
            {
                if (!methods.Value.TryGetValue(toolName, out var method))
                {
                    continue;
                }

                var function = AIFunctionFactory.Create(method, instance, new AIFunctionFactoryOptions
                {
                    Name = toolName,
                    Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    ExcludeResultSchema = true
                });

                tools.Add(new AgentToolDefinition(
                    new AdvancedAnalyticsBoundAiFunction(function, resultCache, maxToolResultCharacters),
                    []));
            }

            return tools;
        }
    }
}
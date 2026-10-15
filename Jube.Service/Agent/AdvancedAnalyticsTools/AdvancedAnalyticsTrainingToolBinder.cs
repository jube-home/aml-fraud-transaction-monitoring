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

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Agent.PageAgentToolBinder;
using Jube.Service.Agent.PageAgentToolBinder.Models;
using log4net;
using Microsoft.Extensions.AI;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    public static class AdvancedAnalyticsTrainingToolBinder
    {
        private static readonly JsonSerializerOptions toolSchemaJson =
            new(AIJsonUtilities.DefaultOptions)
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

        private static readonly Lazy<IReadOnlyDictionary<string, AdvancedAnalyticsToolDescriptor>> byName =
            new(() => AdvancedAnalyticsAgentToolBinder.Descriptors
                .Where(descriptor => !string.IsNullOrWhiteSpace(descriptor.ToolName))
                .GroupBy(descriptor => descriptor.ToolName!, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal));

        public static IReadOnlyDictionary<string, AdvancedAnalyticsToolDescriptor> Descriptors => byName.Value;

        public static IReadOnlyList<AgentToolDefinition> Bind(
            ServiceAgentBindingContext context, IReadOnlyList<string> toolNames, ILog log)
        {
            var cache = new ServiceInstanceCache(context, log);
            var tools = new List<AgentToolDefinition>();

            foreach (var toolName in toolNames)
            {
                if (!byName.Value.TryGetValue(toolName, out var descriptor))
                {
                    throw new InvalidOperationException(
                        $"No [AdvancedAnalyticsTool] method implements the named analytics tool '{toolName}'.");
                }

                if (!TryResolveChain(descriptor.GroupType, out var chain, out var serviceType))
                {
                    continue;
                }

                if (!ServiceInstanceCache.CanConstruct(serviceType, context.Services))
                {
                    continue;
                }

                var function = AIFunctionFactory.Create(descriptor.Method,
                    _ => Materialise(chain, cache.Get(serviceType)),
                    new AIFunctionFactoryOptions
                    {
                        Name = toolName,
                        Description = descriptor.Description,
                        SerializerOptions = toolSchemaJson,
                        ExcludeResultSchema = true
                    });

                tools.Add(new AgentToolDefinition(
                    new AdvancedAnalyticsTrainingBoundAiFunction(function, serviceType, cache,
                        context.ResultCache, context.MaxToolResultCharacters),
                    AdvancedAnalyticsAgentToolBinder.PermissionIdsFor(descriptor.Permissions)));
            }

            return tools;
        }

        public static bool TryResolveChain(Type toolsType, out IReadOnlyList<Type> chain, out Type serviceType)
        {
            var links = new List<Type>();
            var current = toolsType;

            while (true)
            {
                if (links.Count > 4)
                {
                    chain = [];
                    serviceType = typeof(object);

                    return false;
                }

                var dependency = SoleDependency(current);

                if (dependency is null)
                {
                    chain = [];
                    serviceType = typeof(object);

                    return false;
                }

                links.Add(current);

                if (HasFactory(dependency))
                {
                    chain = links;
                    serviceType = dependency;

                    return true;
                }

                var implementation = dependency.IsInterface ? SoleImplementation(dependency) : dependency;

                if (implementation is null)
                {
                    chain = [];
                    serviceType = typeof(object);

                    return false;
                }

                current = implementation;
            }
        }

        private static object Materialise(IReadOnlyList<Type> chain, object service)
        {
            var instance = service;

            for (var index = chain.Count - 1; index >= 0; index--)
            {
                instance = Activator.CreateInstance(chain[index], instance)
                           ?? throw new InvalidOperationException(
                               $"'{chain[index].Name}' could not be constructed around its dependency.");
            }

            return instance;
        }

        private static Type? SoleDependency(Type type)
        {
            var constructors = type.GetConstructors();

            if (constructors.Length != 1)
            {
                return null;
            }

            var parameters = constructors[0].GetParameters();

            return parameters.Length == 1 ? parameters[0].ParameterType : null;
        }

        private static Type? SoleImplementation(Type contract)
        {
            var implementations = contract.Assembly.GetTypes()
                .Where(candidate => candidate is { IsClass: true, IsAbstract: false }
                                    && contract.IsAssignableFrom(candidate))
                .ToList();

            return implementations.Count == 1 ? implementations[0] : null;
        }

        private static bool HasFactory(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(method => method.Name == "CreateAsync"
                               && method.ReturnType.IsGenericType
                               && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                               && method.ReturnType.GetGenericArguments()[0] == type);
        }
    }
}
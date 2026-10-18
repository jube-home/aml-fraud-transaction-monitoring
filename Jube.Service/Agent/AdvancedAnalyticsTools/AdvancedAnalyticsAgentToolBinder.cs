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

using System.Text.Json;
using System.Text.Json.Serialization;
using Jube.AskJooby.Agents.Basic.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Microsoft.Extensions.AI;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    public static class AdvancedAnalyticsAgentToolBinder
    {
        private static readonly Dictionary<string, int> permissionIds = new(StringComparer.Ordinal)
        {
            [AdvancedAnalyticsPermissions.Training] = 66,
            [AdvancedAnalyticsPermissions.Recall] = 46,
            [AdvancedAnalyticsPermissions.PromoteModel] = 47,
            [AdvancedAnalyticsPermissions.Analytics] = 48
        };

        private static readonly JsonSerializerOptions toolSchemaJson =
            new(AIJsonUtilities.DefaultOptions)
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

        private static readonly Lazy<IReadOnlyList<AdvancedAnalyticsToolDescriptor>> catalogue =
            new(() =>
            {
                var descriptors = AdvancedAnalyticsToolCatalogue.Discover(
                    typeof(AdvancedAnalyticsAgentToolBinder).Assembly);
                AdvancedAnalyticsGroupIcons.Register(descriptors);

                return descriptors;
            });

        public static IReadOnlyList<AdvancedAnalyticsToolDescriptor> Descriptors => catalogue.Value;

        public static IReadOnlyList<string> Groups =>
            catalogue.Value.Select(descriptor => descriptor.GroupName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

        public static IReadOnlyList<string> ComputationGroups =>
            catalogue.Value.Where(descriptor => IsPureComputation(descriptor.GroupType))
                .Select(descriptor => descriptor.GroupName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

        public static IReadOnlyList<string> ServiceConstructedGroups =>
            catalogue.Value.Where(descriptor => !IsPureComputation(descriptor.GroupType))
                .Select(descriptor => descriptor.GroupName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

        public static IReadOnlyList<int> PermissionIdsFor(IReadOnlyList<string> permissionNames)
        {
            return permissionNames
                .Select(name => permissionIds.TryGetValue(name, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .OrderBy(id => id)
                .ToList();
        }

        public static bool IsPureComputation(Type groupType)
        {
            return groupType.GetConstructor(Type.EmptyTypes) is not null;
        }

        public static IReadOnlyList<AgentToolDefinition> BindNames(IReadOnlyList<string> toolNames,
            IToolResultCache? resultCache = null, int maxToolResultCharacters = 6000,
            IAdvancedAnalyticsTrainingDataPointerReader? pointerReader = null,
            IAdvancedAnalyticsTrainingDataPointerWriter? pointerWriter = null)
        {
            var wanted = toolNames.ToHashSet(StringComparer.Ordinal);

            return BindDescriptors(
                catalogue.Value.Where(candidate => candidate.ToolName is not null
                                                   && wanted.Contains(candidate.ToolName)),
                resultCache, maxToolResultCharacters, pointerReader, pointerWriter);
        }

        public static IReadOnlyList<AgentToolDefinition> Bind(string groupName,
            IToolResultCache? resultCache = null, int maxToolResultCharacters = 6000,
            IAdvancedAnalyticsTrainingDataPointerReader? pointerReader = null,
            IAdvancedAnalyticsTrainingDataPointerWriter? pointerWriter = null)
        {
            return BindDescriptors(
                catalogue.Value.Where(candidate =>
                    string.Equals(candidate.GroupName, groupName, StringComparison.Ordinal)),
                resultCache, maxToolResultCharacters, pointerReader, pointerWriter);
        }

        private static IReadOnlyList<AgentToolDefinition> BindDescriptors(
            IEnumerable<AdvancedAnalyticsToolDescriptor> descriptors,
            IToolResultCache? resultCache, int maxToolResultCharacters,
            IAdvancedAnalyticsTrainingDataPointerReader? pointerReader = null,
            IAdvancedAnalyticsTrainingDataPointerWriter? pointerWriter = null)
        {
            var instances = new Dictionary<Type, object>();
            var tools = new List<AgentToolDefinition>();

            foreach (var descriptor in descriptors)
            {
                if (string.IsNullOrWhiteSpace(descriptor.ToolName))
                {
                    continue;
                }

                if (!instances.TryGetValue(descriptor.GroupType, out var instance))
                {
                    if (descriptor.GroupType == typeof(ModelSelectionTools) && pointerReader is not null)
                    {
                        instance = new ModelSelectionTools(pointerReader, pointerWriter);
                    }
                    else if (IsPureComputation(descriptor.GroupType))
                    {
                        instance = Activator.CreateInstance(descriptor.GroupType);

                        if (instance is null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    instances[descriptor.GroupType] = instance;
                }

                var function = AIFunctionFactory.Create(descriptor.Method, _ => instance,
                    new AIFunctionFactoryOptions
                    {
                        Name = descriptor.ToolName,
                        Description = descriptor.Description,
                        SerializerOptions = toolSchemaJson,
                        ExcludeResultSchema = true
                    });

                tools.Add(new AgentToolDefinition(
                    new AdvancedAnalyticsBoundAiFunction(function, resultCache, maxToolResultCharacters),
                    PermissionIdsFor(descriptor.Permissions)));
            }

            return tools;
        }
    }
}
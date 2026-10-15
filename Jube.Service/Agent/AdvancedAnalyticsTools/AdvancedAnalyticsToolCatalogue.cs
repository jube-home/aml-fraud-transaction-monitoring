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

using System.ComponentModel;
using System.Reflection;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    public static class AdvancedAnalyticsToolCatalogue
    {
        public const int MinimumDescriptionLength = 30;
        public const int MinimumParameterDescriptionLength = 8;
        private static readonly string libraryName = string.Concat("Acc", "ord");

        public static IReadOnlyList<AdvancedAnalyticsToolDescriptor> Discover(Assembly assembly)
        {
            var descriptors = new List<AdvancedAnalyticsToolDescriptor>();

            foreach (var type in assembly.GetTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                var group = type.GetCustomAttribute<AdvancedAnalyticsToolGroupAttribute>(false);

                if (group == null)
                {
                    continue;
                }

                var groupPermissions = type
                    .GetCustomAttributes<AdvancedAnalyticsRequiresPermissionAttribute>(false)
                    .Select(a => a.Permission)
                    .ToList();

                var methods = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.DeclaredOnly)
                    .OrderBy(m => m.MetadataToken);

                foreach (var method in methods)
                {
                    var tool = method.GetCustomAttribute<AdvancedAnalyticsToolAttribute>(false);

                    if (tool == null)
                    {
                        continue;
                    }

                    var permissions = method
                        .GetCustomAttributes<AdvancedAnalyticsRequiresPermissionAttribute>(false)
                        .Select(a => a.Permission)
                        .Concat(groupPermissions)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                    descriptors.Add(new AdvancedAnalyticsToolDescriptor(
                        group.Name,
                        type,
                        method,
                        tool.Name,
                        method.GetCustomAttribute<DescriptionAttribute>(false)?.Description,
                        permissions));
                }
            }

            return descriptors;
        }

        public static IReadOnlyList<string> Validate(IReadOnlyList<AdvancedAnalyticsToolDescriptor> descriptors)
        {
            var violations = new List<string>();

            foreach (var descriptor in descriptors)
            {
                var label = $"{descriptor.GroupType.FullName}.{descriptor.Method.Name}";

                if (string.IsNullOrWhiteSpace(descriptor.ToolName))
                {
                    violations.Add($"{label}: tool name is empty.");
                }

                var description = descriptor.Description?.Trim();

                if (string.IsNullOrEmpty(description))
                {
                    violations.Add($"{label}: tool description is missing.");
                }
                else
                {
                    if (description.Length < MinimumDescriptionLength)
                    {
                        violations.Add(
                            $"{label}: tool description is shorter than {MinimumDescriptionLength} characters.");
                    }

                    if (ContainsLibraryName(description))
                    {
                        violations.Add($"{label}: tool description names the underlying library.");
                    }
                }

                if (descriptor.Permissions.Count == 0)
                {
                    violations.Add($"{label}: no required permission declared.");
                }

                foreach (var permission in descriptor.Permissions.Where(p =>
                             !AdvancedAnalyticsPermissions.All.Contains(p)))
                {
                    violations.Add($"{label}: unknown permission '{permission}'.");
                }

                foreach (var parameter in descriptor.Method.GetParameters()
                             .Where(p => p.ParameterType != typeof(CancellationToken)))
                {
                    var parameterDescription =
                        parameter.GetCustomAttribute<DescriptionAttribute>(false)?.Description.Trim();

                    if (string.IsNullOrEmpty(parameterDescription))
                    {
                        violations.Add($"{label}: parameter '{parameter.Name}' has no description.");
                    }
                    else
                    {
                        if (parameterDescription.Length < MinimumParameterDescriptionLength)
                        {
                            violations.Add(
                                $"{label}: parameter '{parameter.Name}' description is shorter than {MinimumParameterDescriptionLength} characters.");
                        }

                        if (ContainsLibraryName(parameterDescription))
                        {
                            violations.Add(
                                $"{label}: parameter '{parameter.Name}' description names the underlying library.");
                        }
                    }
                }
            }

            foreach (var duplicate in descriptors
                         .Where(d => !string.IsNullOrWhiteSpace(d.ToolName))
                         .GroupBy(d => d.ToolName!, StringComparer.OrdinalIgnoreCase)
                         .Where(g => g.Count() > 1))
            {
                violations.Add(
                    $"Tool name '{duplicate.Key}' is used by {string.Join(", ", duplicate.Select(d => d.GroupType.FullName + "." + d.Method.Name))}.");
            }

            return violations;
        }

        public static IReadOnlyList<string> FindUncovered(
            IReadOnlyList<AdvancedAnalyticsToolDescriptor> descriptors,
            IReadOnlyCollection<string> coveredNames)
        {
            return descriptors
                .Where(d => !coveredNames.Contains(d.Method.Name, StringComparer.Ordinal)
                            && !coveredNames.Contains(d.GroupType.Name + "." + d.Method.Name, StringComparer.Ordinal))
                .Select(d => $"{d.GroupType.FullName}.{d.Method.Name}")
                .ToList();
        }

        private static bool ContainsLibraryName(string text)
        {
            return text.Contains(libraryName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
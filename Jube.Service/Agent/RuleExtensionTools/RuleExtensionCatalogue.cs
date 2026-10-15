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
using System.Runtime.CompilerServices;
using Jube.Dictionary;
using Jube.Service.Agent.RuleExtensionTools.Models;

namespace Jube.Service.Agent.RuleExtensionTools
{
    public static class RuleExtensionCatalogue
    {
        private static readonly Lazy<IReadOnlyList<RuleExtensionSignature>> compiledExtensions =
            new(DiscoverCompiledExtensions);

        public static IReadOnlyList<RuleExtensionSignature> All =>
            [.. compiledExtensions.Value, .. DiscoverDynamicEvaluations()];

        public static IReadOnlyList<string> InstanceTypes =>
            All.Select(signature => signature.Instance)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

        private static IReadOnlyList<RuleExtensionSignature> DiscoverCompiledExtensions()
        {
            var signatures = new List<RuleExtensionSignature>();

            foreach (var method in typeof(Dictionary.Extensions.Extensions)
                         .GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (string.Equals(method.Name, "Eval", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!method.IsDefined(typeof(ExtensionAttribute), false))
                {
                    continue;
                }

                var parameters = method.GetParameters();

                if (parameters.Length == 0)
                {
                    continue;
                }

                signatures.Add(new RuleExtensionSignature(
                    method.Name,
                    RuleExtensionVisualBasicTypes.Describe(parameters[0].ParameterType),
                    parameters.Skip(1).Select(Describe).ToList(),
                    RuleExtensionVisualBasicTypes.Describe(method.ReturnType),
                    RuleExtensionOrigin.Extension));
            }

            return signatures
                .OrderBy(signature => signature.Instance, StringComparer.Ordinal)
                .ThenBy(signature => signature.Name, StringComparer.Ordinal)
                .ThenBy(signature => signature.Parameters.Count)
                .ToList();
        }

        private static IReadOnlyList<RuleExtensionSignature> DiscoverDynamicEvaluations()
        {
            if (!EvalExpressionRegistry.Enabled)
            {
                return [];
            }

            var signatures = new List<RuleExtensionSignature>();

            foreach (var (name, resultTypeId) in EvalExpressionRegistry.Entries)
            {
                if (!EvalExpressionRegistry.ResultTypes.TryGetValue(resultTypeId, out var resultType))
                {
                    continue;
                }

                signatures.Add(new RuleExtensionSignature(
                    name,
                    "String",
                    [],
                    resultType.VbKeyword,
                    RuleExtensionOrigin.DynamicEvaluation));
            }

            return signatures.OrderBy(signature => signature.Name, StringComparer.Ordinal).ToList();
        }

        private static RuleExtensionParameter Describe(ParameterInfo parameter)
        {
            return new RuleExtensionParameter(
                parameter.Name ?? "value",
                RuleExtensionVisualBasicTypes.Describe(parameter.ParameterType),
                parameter.IsOptional,
                parameter.IsOptional && parameter.HasDefaultValue
                    ? RuleExtensionVisualBasicTypes.DescribeDefault(parameter.DefaultValue)
                    : null,
                parameter.IsDefined(typeof(ParamArrayAttribute), false));
        }
    }
}
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

using System.Reflection;
using Jube.Service.Agent.Models;
using Jube.Service.Agent.PageAgentToolBinder.Models;

namespace Jube.Service.Agent.PageAgentToolBinder
{
    internal static class ServiceOperationIndex
    {
        private static readonly Lazy<IReadOnlyDictionary<string, ServiceOperationBinding>> bindingsLazy =
            new(Build);

        internal static IReadOnlyDictionary<string, ServiceOperationBinding> All => bindingsLazy.Value;

        internal static ServiceOperationBinding Get(string toolName)
        {
            if (!All.TryGetValue(toolName, out var binding))
            {
                throw new InvalidOperationException(
                    $"No [ServiceOperation] method implements the catalogued tool '{toolName}'.");
            }

            return binding;
        }

        private static IReadOnlyDictionary<string, ServiceOperationBinding> Build()
        {
            var bindings = new Dictionary<string, ServiceOperationBinding>(StringComparer.Ordinal);

            foreach (var type in typeof(ServiceOperationAttribute).Assembly.GetTypes()
                         .Where(t => t is { IsClass: true, IsAbstract: false }))
            {
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    var attribute = method.GetCustomAttribute<ServiceOperationAttribute>();

                    if (attribute is null)
                    {
                        continue;
                    }

                    if (bindings.ContainsKey(attribute.Name))
                    {
                        throw new InvalidOperationException(
                            $"Two [ServiceOperation] methods both claim the name '{attribute.Name}'.");
                    }

                    bindings[attribute.Name] = new ServiceOperationBinding(type, method, attribute);
                }
            }

            ServiceOperationIcons.Register(bindings.Keys);

            return bindings;
        }
    }
}
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

namespace Jube.Engine.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class AnchoredAssemblySerializationBinder : DefaultSerializationBinder
    {
        private readonly HashSet<Assembly> allowedAssemblies;
        private readonly HashSet<string> allowedFamilies;

        public AnchoredAssemblySerializationBinder(params Type[] anchorTypes)
        {
            allowedAssemblies = [.. anchorTypes.Select(t => t.Assembly)];
            allowedFamilies =
            [
                .. anchorTypes.Select(t => t.Assembly.GetName().Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Select(n => n!.Split('.')[0])
            ];
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            var type = base.BindToType(assemblyName, typeName);

            if (!IsAllowed(type))
            {
                throw new JsonSerializationException(
                    $"The type '{typeName}' is not permitted in stored model JSON.");
            }

            return type;
        }

        private bool IsAllowed(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.IsArray)
            {
                return IsAllowed(type.GetElementType());
            }

            if (type.IsGenericType && !type.GetGenericArguments().All(IsAllowed))
            {
                return false;
            }

            return type.IsPrimitive || type == typeof(string) || allowedAssemblies.Contains(type.Assembly) ||
                   IsInAnchorFamily(type.Assembly);
        }

        private bool IsInAnchorFamily(Assembly assembly)
        {
            var name = assembly.GetName().Name;

            return !string.IsNullOrEmpty(name) && allowedFamilies.Contains(name.Split('.')[0]);
        }
    }
}

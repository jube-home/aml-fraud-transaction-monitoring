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
using System.Collections.Generic;
using System.Reflection;

namespace Jube.Test.AdvancedAnalytics.Conventions
{
    public static class CoverageScanner
    {
        public static IReadOnlyCollection<string> CoveredNames(Assembly assembly)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            foreach (var type in assembly.GetTypes())
            {
                foreach (var attribute in type.GetCustomAttributesData())
                {
                    Collect(attribute, names);
                }

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                       BindingFlags.Instance | BindingFlags.Static |
                                                       BindingFlags.DeclaredOnly))
                foreach (var attribute in method.GetCustomAttributesData())
                {
                    Collect(attribute, names);
                }
            }

            return names;
        }

        private static void Collect(CustomAttributeData attribute, HashSet<string> names)
        {
            if (attribute.AttributeType.Name != "TraitAttribute" || attribute.ConstructorArguments.Count != 2)
            {
                return;
            }

            if ((string?)attribute.ConstructorArguments[0].Value == "Covers" &&
                attribute.ConstructorArguments[1].Value is string value)
            {
                names.Add(value);
            }
        }
    }
}
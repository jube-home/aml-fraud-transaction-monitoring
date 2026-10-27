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
using System.Runtime.CompilerServices;
using FluentValidation;

namespace Jube.Service.Query.FormModel
{
    public static class ValidatorBuilder
    {
        public static IValidator Build(Type validator)
        {
            var constructor = validator.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
            var arguments = constructor.GetParameters().Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : Placeholder(parameter.ParameterType))
                .ToArray();

            return (IValidator)constructor.Invoke(arguments);
        }

        private static object? Placeholder(Type type)
        {
            if (type == typeof(string))
            {
                return string.Empty;
            }

            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            if (type.IsInterface)
            {
                return typeof(DispatchProxy).GetMethods()
                    .Single(method => method.Name == nameof(DispatchProxy.Create) && method.GetGenericArguments().Length == 2)
                    .MakeGenericMethod(type, typeof(DefaultReturnProxy))
                    .Invoke(null, null);
            }

            return type.IsAbstract ? null : RuntimeHelpers.GetUninitializedObject(type);
        }
    }
}

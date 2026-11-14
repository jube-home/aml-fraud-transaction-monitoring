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

namespace Jube.App.Wire
{
    public static class ColumnarRows
    {
        public static bool TryGetValueType(Type type, out Type valueType)
        {
            valueType = null;

            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>))
            {
                return false;
            }

            var row = type.GetGenericArguments()[0];

            if (!row.IsGenericType || row.GetGenericTypeDefinition() != typeof(Dictionary<,>)
                                   || row.GetGenericArguments()[0] != typeof(string)
                                   || row.GetGenericArguments()[1].IsValueType)
            {
                return false;
            }

            valueType = row.GetGenericArguments()[1];

            return true;
        }
    }
}

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
using System.Globalization;
using Jube.Analytics.Categorical;
using Jube.Dictionary;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.Exhaustive.Utilities
{
    public static class CategoricalValue
    {
        public static string FromToken(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            switch (token.Type)
            {
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Integer:
                    return Convert.ToString(((JValue)token).Value, CultureInfo.InvariantCulture);
                case JTokenType.Float:
                    return CategoricalNormaliser.ToRaw(token.Value<double>());
                case JTokenType.Boolean:
                    return CategoricalNormaliser.ToRaw(token.Value<bool>());
                case JTokenType.Date:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                    return ((JValue)token).ToString(CultureInfo.InvariantCulture);
                default:
                    return null;
            }
        }

        public static string FromInternalValue(InternalValue value)
        {
            switch (value.Type)
            {
                case InternalValue.ValueType.String:
                    return value.AsString();
                case InternalValue.ValueType.Int:
                    return CategoricalNormaliser.ToRaw(value.AsInt());
                case InternalValue.ValueType.Double:
                    return CategoricalNormaliser.ToRaw(value.AsDouble());
                case InternalValue.ValueType.Bool:
                    return CategoricalNormaliser.ToRaw(value.AsBool());
                case InternalValue.ValueType.DateTime:
                    return value.AsDateTime().ToString("o", CultureInfo.InvariantCulture);
                case InternalValue.ValueType.Guid:
                    return value.AsGuid().ToString("D");
                default:
                    return null;
            }
        }
    }
}
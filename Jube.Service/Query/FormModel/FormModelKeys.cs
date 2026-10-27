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

namespace Jube.Service.Query.FormModel
{
    public static class FormModelKeys
    {
        public static string Field(string area, string field)
        {
            return $"{area}.{field}";
        }

        public static string Help(string area, string field)
        {
            return $"{area}.{field}.help";
        }

        public static string Description(string area, string field)
        {
            return $"{area}.{field}.description";
        }

        public static string SourceHash(string english)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(english.Trim()));

            return Convert.ToHexString(bytes, 0, 4).ToLowerInvariant();
        }

        public static string Placeholder(string area, string field)
        {
            return $"{area}.{field}.placeholder";
        }

        public static string Group(string area, string path)
        {
            return $"{area}.group.{path}";
        }

        public static string Child(string area, string childArea)
        {
            return $"{area}.child.{childArea}";
        }

        public static string Hub(string area, string hub)
        {
            return $"{area}.hub.{hub}";
        }

        public static string Filter(string area, string param)
        {
            return $"{area}.filter.{param}";
        }

        public static string Title(string area)
        {
            return $"{area}.title";
        }

        public static string Column(string area, string field)
        {
            return $"{area}.column.{field}";
        }

        public static string Option(string key)
        {
            return $"option.{key}";
        }
    }
}

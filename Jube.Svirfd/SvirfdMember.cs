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
using System.Linq;
using Svirfd;

namespace Jube.Svirfd
{
    public sealed class SvirfdMember
    {
        private readonly Func<IpResponse, object?> read;

        public SvirfdMember(string name, int dataTypeId, string sqlCast, IReadOnlyList<string> jsonSegments,
            bool array, bool isVolatile, Func<IpResponse, object?> read)
        {
            Name = name;
            DataTypeId = dataTypeId;
            SqlCast = sqlCast;
            JsonSegments = jsonSegments;
            Array = array;
            Volatile = isVolatile;
            this.read = read;
            Segments = name.Split('.');
            JsonPath = string.Join(".", jsonSegments)
                .Replace(".[", "[");
        }

        public string Name { get; }
        public int DataTypeId { get; }
        public string SqlCast { get; }
        public IReadOnlyList<string> JsonSegments { get; }
        public bool Array { get; }
        public bool Volatile { get; }
        public IReadOnlyList<string> Segments { get; }
        public string JsonPath { get; }

        public string JQueryBuilderDataType => DataTypeId switch
        {
            SvirfdMembers.IntegerType => "integer",
            SvirfdMembers.DoubleType => "double",
            SvirfdMembers.DateType => "datetime",
            SvirfdMembers.BooleanType => "boolean",
            _ => "string"
        };

        public object? Read(IpResponse record)
        {
            return read(record);
        }

        public string SqlPath(string entityLiteral)
        {
            var root = $"\"Json\"->'svirfd'->'{entityLiteral}'";

            if (Array)
            {
                var container = string.Concat(JsonSegments.Select(Step));

                return $"jsonb_array_length({root}{container})";
            }

            var path = string.Concat(JsonSegments.Take(JsonSegments.Count - 1).Select(Step));
            var last = JsonSegments[^1];
            var text = last.StartsWith('[') ? $"->>{last.Trim('[', ']')}" : $"->>'{last}'";

            return string.IsNullOrEmpty(SqlCast) ? $"({root}{path}{text})" : $"({root}{path}{text}){SqlCast}";
        }

        private static string Step(string segment)
        {
            return segment.StartsWith('[') ? $"->{segment.Trim('[', ']')}" : $"->'{segment}'";
        }
    }
}

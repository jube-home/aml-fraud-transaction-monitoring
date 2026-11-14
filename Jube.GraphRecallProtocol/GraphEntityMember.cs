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

namespace Jube.GraphRecallProtocol
{
    public sealed class GraphEntityMember
    {
        private readonly Func<GraphEntity, object> read;
        private readonly Action<GraphEntity, object> write;

        public GraphEntityMember(string name, int dataTypeId, string sqlCast, Func<GraphEntity, object> read,
            Action<GraphEntity, object> write)
        {
            Name = name;
            DataTypeId = dataTypeId;
            SqlCast = sqlCast;
            this.read = read;
            this.write = write;
            Segments = name.Split('.');
            JsonPath = string.Join(".", Segments.Select(GraphEntityMembers.CamelCase));
        }

        public string Name { get; }
        public int DataTypeId { get; }
        public string SqlCast { get; }
        public IReadOnlyList<string> Segments { get; }
        public string JsonPath { get; }

        public string JQueryBuilderDataType => DataTypeId switch
        {
            GraphEntityMembers.IntegerType => "integer",
            GraphEntityMembers.DoubleType => "double",
            GraphEntityMembers.BooleanType => "boolean",
            _ => "string"
        };

        public object Read(GraphEntity entity)
        {
            return read(entity);
        }

        public void Write(GraphEntity entity, object value)
        {
            write(entity, value);
        }

        public string SqlPath(string entityLiteral)
        {
            var path = string.Join(" -> ", Segments.Take(Segments.Count - 1).Select(s => $"'{GraphEntityMembers.CamelCase(s)}'"));

            return
                $"(\"Json\"-> 'graph' -> '{entityLiteral}'{(path.Length == 0 ? string.Empty : " -> " + path)} ->> '{GraphEntityMembers.CamelCase(Segments[^1])}'){SqlCast}";
        }
    }
}

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

namespace Jube.Data.Query.ArchiveKeyLookup
{
    using System;
    using Dto.Options;

    public sealed record ArchiveKeyPredicate(
        string Key,
        byte ProcessingTypeId,
        ArchiveKeyValueColumn Column,
        ArchiveKeyLookupOperator Operator,
        object Value,
        object Upper = null,
        bool EntryKey = false)
    {
        public const byte RequestXPathProcessingTypeId = 1;

        public string ColumnName => ColumnNameOf(Column);

        public static string ColumnNameOf(ArchiveKeyValueColumn column)
        {
            return column switch
            {
                ArchiveKeyValueColumn.Integer => "KeyValueInteger",
                ArchiveKeyValueColumn.Float => "KeyValueFloat",
                ArchiveKeyValueColumn.Boolean => "KeyValueBoolean",
                ArchiveKeyValueColumn.Date => "KeyValueDate",
                ArchiveKeyValueColumn.Long => "KeyValueLong",
                _ => "KeyValueString"
            };
        }

        public static ArchiveKeyValueColumn ColumnOf(int? dataTypeId)
        {
            return dataTypeId switch
            {
                2 => ArchiveKeyValueColumn.Integer,
                3 or 6 or 7 => ArchiveKeyValueColumn.Float,
                4 => ArchiveKeyValueColumn.Date,
                5 => ArchiveKeyValueColumn.Boolean,
                8 => ArchiveKeyValueColumn.String,
                _ => ArchiveKeyValueColumn.String
            };
        }

        public static bool AllowsRange(ArchiveKeyValueColumn column)
        {
            return column is ArchiveKeyValueColumn.Integer or ArchiveKeyValueColumn.Float
                or ArchiveKeyValueColumn.Date or ArchiveKeyValueColumn.Long;
        }

        public static ArchiveKeyPredicate RequestXPath(string key, ArchiveKeyValueColumn column, object value)
        {
            return new ArchiveKeyPredicate(key, RequestXPathProcessingTypeId, column, ArchiveKeyLookupOperator.Equal,
                value);
        }

        public static ArchiveKeyPredicate ForEntryKey(string value)
        {
            return new ArchiveKeyPredicate(string.Empty, 0, ArchiveKeyValueColumn.String,
                ArchiveKeyLookupOperator.Equal, value, EntryKey: true);
        }

        public string Condition(string alias, Func<object, string> bind)
        {
            if (EntryKey)
            {
                return $"{alias}.\"EntryKeyValue\" = {bind(Value)}";
            }

            var column = $"{alias}.\"{ColumnName}\"";
            var processingType = bind((short)ProcessingTypeId);
            var key = bind(Key);

            var comparison = Operator switch
            {
                ArchiveKeyLookupOperator.GreaterThan => $"{column} > {bind(Value)}",
                ArchiveKeyLookupOperator.GreaterThanOrEqual => $"{column} >= {bind(Value)}",
                ArchiveKeyLookupOperator.LessThan => $"{column} < {bind(Value)}",
                ArchiveKeyLookupOperator.LessThanOrEqual => $"{column} <= {bind(Value)}",
                ArchiveKeyLookupOperator.Between => $"{column} between {bind(Value)} and {bind(Upper)}",
                _ => $"{column} = {bind(Value)}"
            };

            return $"{alias}.\"ProcessingTypeId\" = {processingType} and {alias}.\"Key\" = {key} and {comparison}";
        }
    }
}

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
using LinqToDB;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class CaseWorkflowDispatch
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseId { get; set; }
        [Column] [Nullable] [Key(3)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(4)] public string CaseKeyValue { get; set; }
        [Column] [Nullable] [Key(5)] public byte? SourceKindId { get; set; }
        [Column] [Nullable] [Key(6)] public int? SourceId { get; set; }
        [Column] [Nullable] [Key(7)] public string SourceName { get; set; }
        [Column] [Nullable] [Key(8)] public byte? ChannelId { get; set; }
        [Column] [Nullable] [Key(9)] public byte? ModeId { get; set; }
        [Column] [Nullable] [Key(10)] public string Destination { get; set; }
        [Column] [Nullable] [Key(11)] public string Subject { get; set; }
        [Column] [Nullable] [Key(12)] public string Body { get; set; }
        [Column] [Nullable] [Key(13)] public byte? OutcomeId { get; set; }
        [Column] [Nullable] [Key(14)] public string Detail { get; set; }
        [Column] [Nullable] [Key(15)] public int? ResponseStatus { get; set; }
        [Column] [Nullable] [Key(16)] public Guid CorrelationId { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(17)]
        public string Rows { get; set; }

        [Column] [Nullable] [Key(18)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(19)] public DateTime? CreatedDate { get; set; }
    }
}

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
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class CaseReport
    {
        [Column] [PrimaryKey] [Identity] [Key(0)] public long Id { get; set; }
        [Column] [Key(1)] public Guid Guid { get; set; }
        [Column] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Key(3)] public int CaseId { get; set; }
        [Column] [Nullable] [Key(4)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(5)] public string CaseKeyValue { get; set; }
        [Column] [Key(6)] public string ProfileName { get; set; }
        [Column] [Key(7)] public int ProfileVersion { get; set; }
        [Column] [Key(8)] public string ReportCode { get; set; }
        [Column] [Key(9)] public short StateId { get; set; }
        [Column] [Key(10)] public int CurrentVersion { get; set; }
        [Column] [Nullable] [Key(11)] public string FiuReference { get; set; }
        [Column] [Key(12)] public string CreatedUser { get; set; }
        [Column] [Key(13)] public DateTime CreatedDate { get; set; }
        [Column] [Nullable] [Key(14)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(16)] public string SubmittedUser { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? SubmittedDate { get; set; }
    }
}

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
    public class CaseWorkflowReleaseHookVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowReleaseHookId { get; set; }
        [Column] [Nullable] [Key(2)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(3)] public Guid CaseWorkflowGuid { get; set; }
        [Column] [Nullable] [Key(4)] public string Name { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public string SourceField { get; set; }
        [Column] [Nullable] [Key(8)] public string SourceValue { get; set; }
        [Column] [Nullable] [Key(9)] public string ReleaseUrl { get; set; }
        [Column] [Nullable] [Key(10)] public string RefuseUrl { get; set; }
        [Column] [Nullable] [Key(11)] public byte? HttpEndpointTypeId { get; set; }
        [Column] [Nullable] [Key(12)] public string HeaderName { get; set; }
        [Column] [Nullable] [Key(13)] public string CredentialVariable { get; set; }
        [Column] [Nullable] [Key(14)] public string Body { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(16)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(18)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(19)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(20)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(21)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(22)] public int? Version { get; set; }
        [Column] [Nullable] [Key(23)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(24)] public int? ImportId { get; set; }
    }
}

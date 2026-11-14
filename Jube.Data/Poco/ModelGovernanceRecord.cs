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
    public class ModelGovernanceRecord
    {
        [Column] [PrimaryKey] [Identity] [Key(0)] public long Id { get; set; }
        [Column] [Key(1)] public int TenantRegistryId { get; set; }
        [Column] [Key(2)] public short KindId { get; set; }
        [Column] [Key(3)] public Guid ModelGuid { get; set; }
        [Column] [Key(4)] public int Version { get; set; }
        [Column] [Key(5)] public string OwnerUser { get; set; }
        [Column] [Key(6)] public string IntendedPurpose { get; set; }
        [Column] [Key(7)] public short RiskTierId { get; set; }
        [Column] [Key(8)] public string HumanOversight { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? NextReviewDate { get; set; }
        [Column] [Key(10)] public string CreatedUser { get; set; }
        [Column] [Key(11)] public DateTime CreatedDate { get; set; }
    }
}

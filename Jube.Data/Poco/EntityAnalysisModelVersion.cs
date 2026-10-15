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
    public class EntityAnalysisModelVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(5)] public string EntryXPath { get; set; }
        [Column] [Nullable] [Key(6)] public string ReferenceDateXPath { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(14)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(15)] public string EntryName { get; set; }
        [Column] [Nullable] [Key(16)] public string ReferenceDateName { get; set; }
        [Column] [Nullable] [Key(17)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(18)] public int? CacheFetchLimit { get; set; }
        [Column] [Nullable] [Key(19)] public byte? ReferenceDatePayloadLocationTypeId { get; set; }
        [Column] [Nullable] [Key(20)] public double? MaxResponseElevation { get; set; }
        [Column] [Nullable] [Key(21)] public char? MaxResponseElevationInterval { get; set; }
        [Column] [Nullable] [Key(22)] public int? MaxResponseElevationValue { get; set; }
        [Column] [Nullable] [Key(23)] public int? MaxResponseElevationThreshold { get; set; }
        [Column] [Nullable] [Key(24)] public char? MaxActivationWatcherInterval { get; set; }
        [Column] [Nullable] [Key(25)] public int? MaxActivationWatcherValue { get; set; }
        [Column] [Nullable] [Key(26)] public int? MaxActivationWatcherThreshold { get; set; }
        [Column] [Nullable] [Key(27)] public double? ActivationWatcherSample { get; set; }
        [Column] [Nullable] [Key(28)] public byte? EnableCache { get; set; }
        [Column] [Nullable] [Key(29)] public byte? EnableTtlCounter { get; set; }
        [Column] [Nullable] [Key(30)] public byte? EnableSanctionCache { get; set; }
        [Column] [Nullable] [Key(31)] public byte? EnableRdbmsArchive { get; set; }
        [Column] [Nullable] [Key(32)] public int? Version { get; set; }
        [Column] [Nullable] [Key(33)] public byte? EnableImplicitAsync { get; set; }
        [Column] [Nullable] [Key(34)] public int? ImplicitAsyncTimeoutMilliseconds { get; set; }
        [Column] [Nullable] [Key(35)] public byte? EnableTrace { get; set; }
        [Column] [Nullable] [Key(36)] public byte? EnableLogsInfo { get; set; }
        [Column] [Nullable] [Key(37)] public int? LogsWarnThresholdMilliseconds { get; set; }
        [Column] [Nullable] [Key(38)] public byte? EnableLogsInResponse { get; set; }
        [Column] [Nullable] [Key(39)] public byte? EnableSampling { get; set; }
        [Column] [Nullable] [Key(40)] public double? SamplePercentage { get; set; }
        [Column] [Nullable] [Key(41)] public byte? EnableLogsWarnThreshold { get; set; }
        [Column] [Nullable] [Key(42)] public byte? EnableLogs { get; set; }
    }
}
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
    public class EntityAnalysisModelProcessingCounter
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public string Instance { get; set; }
        [Column] [Nullable] [Key(3)] public int? ModelInvoke { get; set; }
        [Column] [Nullable] [Key(4)] public int? GatewayMatch { get; set; }
        [Column] [Nullable] [Key(5)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(6)] public int? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(7)] public double? ResponseElevationSum { get; set; }
        [Column] [Nullable] [Key(8)] public double? ActivationWatcher { get; set; }
        [Column] [Nullable] [Key(9)] public int? ResponseElevationValueLimit { get; set; }
        [Column] [Nullable] [Key(10)] public int? ResponseElevationLimit { get; set; }
        [Column] [Nullable] [Key(11)] public int? ResponseElevationValueGatewayLimit { get; set; }

        [Association(ThisKey = "EntityAnalysisModelGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(12)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Key(13)] [Column] [Nullable] public long ModelTotalResponseTime { get; set; }
        [Column] [Nullable] [Key(14)] public int? ImplicitAsyncInvoke { get; set; }
        [Column] [Nullable] [Key(15)] public int? ImplicitAsyncTimeout { get; set; }
        [Column] [Nullable] [Key(16)] public int? ImplicitAsyncCompletedAfterTimeout { get; set; }
        [Column] [Nullable] [Key(17)] public int? ImplicitAsyncFaultedAfterTimeout { get; set; }
        [Column] [Nullable] [Key(18)] public long? MinResponseTimeMicroseconds { get; set; }
        [Column] [Nullable] [Key(19)] public long? MaxResponseTimeMicroseconds { get; set; }
        [Column] [Nullable] [Key(20)] public int? ArchiveWalPendingCount { get; set; }
    }
}
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
    public class EtcdMemberStatus
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? OccurredDate { get; set; }
        [Column] [Nullable] [Key(2)] public string Endpoint { get; set; }
        [Column] [Nullable] [Key(3)] public string MemberId { get; set; }
        [Column] [Nullable] [Key(4)] public string Name { get; set; }
        [Column] [Nullable] [Key(5)] public string PeerUrls { get; set; }
        [Column] [Nullable] [Key(6)] public string ClientUrls { get; set; }
        [Column] [Nullable] [Key(7)] public bool? IsLearner { get; set; }
        [Column] [Nullable] [Key(8)] public string Version { get; set; }
        [Column] [Nullable] [Key(9)] public string ClusterVersion { get; set; }
        [Column] [Nullable] [Key(10)] public bool? HealthOk { get; set; }
        [Column] [Nullable] [Key(11)] public string HealthReason { get; set; }
        [Column] [Nullable] [Key(12)] public string LeaderId { get; set; }
        [Column] [Nullable] [Key(13)] public bool? IsLeader { get; set; }
        [Column] [Nullable] [Key(14)] public bool? HasLeader { get; set; }
        [Column] [Nullable] [Key(15)] public long? LeaderChangesTotal { get; set; }
        [Column] [Nullable] [Key(16)] public long? DbSizeBytes { get; set; }
        [Column] [Nullable] [Key(17)] public long? DbSizeInUseBytes { get; set; }
        [Column] [Nullable] [Key(18)] public long? RaftIndex { get; set; }
        [Column] [Nullable] [Key(19)] public long? RaftTerm { get; set; }
        [Column] [Nullable] [Key(20)] public long? RaftAppliedIndex { get; set; }
        [Column] [Nullable] [Key(21)] public long? ProposalsCommittedTotal { get; set; }
        [Column] [Nullable] [Key(22)] public long? ProposalsAppliedTotal { get; set; }
        [Column] [Nullable] [Key(23)] public long? ProposalsPendingCount { get; set; }
        [Column] [Nullable] [Key(24)] public long? ProposalsFailedTotal { get; set; }
        [Column] [Nullable] [Key(25)] public double? WalFsyncAvgMicroseconds { get; set; }
        [Column] [Nullable] [Key(26)] public double? BackendCommitAvgMicroseconds { get; set; }
        [Column] [Nullable] [Key(27)] public long? SlowApplyTotal { get; set; }
        [Column] [Nullable] [Key(28)] public long? SlowReadIndexesTotal { get; set; }
        [Column] [Nullable] [Key(29)] public int? AlarmCount { get; set; }
        [Column] [Nullable] [Key(30)] public string Alarms { get; set; }
        [Column] [Nullable] [Key(31)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(32)] public string Instance { get; set; }
    }
}
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

using System.ComponentModel;
using Jube.Dto.Forms;
using Jube.Dto.Options;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.EtcdMemberStatus
{
    [GridEndpoint("EtcdMemberStatus", Title = "Etcd member status", DefaultSort = "occurredDate")]
    [GridSearch(Label = "Endpoint, name, health reason, alarms contains")]
    [MessagePackObject]
    public sealed record EtcdMemberStatusDto(
        [property: Key(0)] [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Key(1)] [property: Description("UTC timestamp this etcd member was sampled.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Key(2)] [property: Description("The host:port endpoint polled for this row (discovered or explicitly configured).")]
        [property: ListColumn(Order = 20)]
        string Endpoint,
        [property: Key(3)] [property:
            Description(
                "This member's own etcd cluster member id (decimal, can exceed a 64-bit signed integer so kept as text).")]
        [property: ListColumn(Order = 30)]
        string MemberId,
        [property: Key(4)] [property: Description("This member's configured name.")]
        [property: ListColumn(Order = 40)]
        string Name,
        [property: Key(5)] [property: Description("Comma-separated peer URLs this member advertises to other members.")]
        [property: ListColumn(Order = 50)]
        string PeerUrls,
        [property: Key(6)] [property: Description("Comma-separated client URLs this member advertises to clients.")]
        [property: ListColumn(Order = 60)]
        string ClientUrls,
        [property: Key(7)] [property:
            Description(
                "Whether this member is a non-voting learner (added via member add --learner, not yet promoted).")]
        [property: ListColumn(Order = 70)]
        bool? IsLearner,
        [property: Key(8)] [property: Description("This member's etcdserver version.")]
        [property: ListColumn(Order = 80)]
        string Version,
        [property: Key(9)] [property: Description("The etcd cluster's negotiated protocol version, as reported by this member.")]
        [property: ListColumn(Order = 90)]
        string ClusterVersion,
        [property: Key(10)] [property: Description("Whether this member reported itself healthy on GET /health at sample time.")]
        [property: ListColumn(Order = 100)]
        bool? HealthOk,
        [property: Key(11)] [property: Description("The reason string GET /health returned when not healthy; null when healthy.")]
        [property: ListColumn(Order = 110)]
        string HealthReason,
        [property: Key(12)] [property:
            Description(
                "The member id this node currently believes is the raft leader -- compare across every member's row in the same OccurredDate minute to spot disagreement (a split-brain symptom).")]
        [property: ListColumn(Order = 120)]
        string LeaderId,
        [property: Key(13)] [property:
            Description(
                "Whether this member's own MemberId matches LeaderId -- true means this member believes itself to be the current leader.")]
        [property: ListColumn(Order = 130)]
        bool? IsLeader,
        [property: Key(14)] [property:
            Description(
                "Whether this member currently has any leader at all (from the etcd_server_has_leader metric) -- false means this member is mid-election or partitioned from the rest of the cluster.")]
        [property: ListColumn(Order = 140)]
        bool? HasLeader,
        [property: Key(15)] [property:
            Description(
                "Cumulative count of leader elections this member has observed since it started -- etcd's own counter, never reset by Jube; a climbing number across samples means the cluster is flapping leadership.")]
        [property: ListColumn(Order = 150, Format = GridFormat.Integer)]
        long? LeaderChangesTotal,
        [property: Key(16)] [property: Description("Total size of this member's backend database file, in bytes.")]
        [property: ListColumn(Order = 160, Title = "DB Size", Unit = "B", Format = GridFormat.Integer)]
        long? DbSizeBytes,
        [property: Key(17)] [property:
            Description(
                "Size of this member's backend database actually in use, in bytes -- the gap between this and DbSizeBytes is reclaimable via compaction+defrag.")]
        [property: ListColumn(Order = 170, Title = "DB Size In Use", Unit = "B", Format = GridFormat.Integer)]
        long? DbSizeInUseBytes,
        [property: Key(18)] [property: Description("This member's raft log index.")]
        [property: ListColumn(Order = 180, Format = GridFormat.Integer)]
        long? RaftIndex,
        [property: Key(19)] [property:
            Description(
                "This member's current raft term -- a climbing term without a corresponding LeaderChangesTotal increase on other members can indicate a struggling/partitioned member repeatedly calling elections.")]
        [property: ListColumn(Order = 190, Format = GridFormat.Integer)]
        long? RaftTerm,
        [property: Key(20)] [property:
            Description(
                "The raft index this member has actually applied to its state machine -- a growing gap from RaftIndex means this member is falling behind applying committed entries.")]
        [property: ListColumn(Order = 200, Format = GridFormat.Integer)]
        long? RaftAppliedIndex,
        [property: Key(21)] [property:
            Description("Cumulative count of raft proposals committed, etcd's own counter, never reset by Jube.")]
        [property: ListColumn(Order = 210, Format = GridFormat.Integer)]
        long? ProposalsCommittedTotal,
        [property: Key(22)] [property: Description("Cumulative count of raft proposals applied, etcd's own counter, never reset by Jube.")]
        [property: ListColumn(Order = 220, Format = GridFormat.Integer)]
        long? ProposalsAppliedTotal,
        [property: Key(23)] [property:
            Description(
                "Number of raft proposals currently pending on this member -- a sustained non-zero value means this member (or the cluster) cannot keep up with the write rate.")]
        [property: ListColumn(Order = 230, Format = GridFormat.Integer)]
        long? ProposalsPendingCount,
        [property: Key(24)] [property:
            Description(
                "Cumulative count of raft proposals that failed, etcd's own counter, never reset by Jube -- any non-zero value is worth investigating.")]
        [property: ListColumn(Order = 240, Format = GridFormat.Integer)]
        long? ProposalsFailedTotal,
        [property: Key(25)] [property:
            Description(
                "Average WAL fsync latency in microseconds since this member started (etcd's own cumulative sum/count, not a per-minute figure) -- etcd's own documentation treats sustained values above roughly 10 milliseconds as a sign the disk cannot keep up.")]
        [property: ListColumn(Order = 250, Title = "WAL Fsync Avg", Unit = "µs", Format = GridFormat.Decimal)]
        double? WalFsyncAvgMicroseconds,
        [property: Key(26)] [property:
            Description(
                "Average backend (bolt db) commit latency in microseconds since this member started, same cumulative caveat as WalFsyncAvgMicroseconds.")]
        [property: ListColumn(Order = 260, Title = "Backend Commit Avg", Unit = "µs", Format = GridFormat.Decimal)]
        double? BackendCommitAvgMicroseconds,
        [property: Key(27)] [property:
            Description(
                "Cumulative count of 'slow apply' warnings (applying a raft entry took too long), etcd's own counter, never reset by Jube.")]
        [property: ListColumn(Order = 270, Format = GridFormat.Integer)]
        long? SlowApplyTotal,
        [property: Key(28)] [property:
            Description(
                "Cumulative count of 'slow read index' warnings (a linearizable read took too long), etcd's own counter, never reset by Jube.")]
        [property: ListColumn(Order = 280, Format = GridFormat.Integer)]
        long? SlowReadIndexesTotal,
        [property: Key(29)] [property:
            Description(
                "Number of active etcd alarms on this member (e.g. NOSPACE when the backend quota is exceeded, CORRUPT when a data corruption is detected) -- any non-zero value here means etcd itself is refusing some operations.")]
        [property: ListColumn(Order = 290, Format = GridFormat.Integer)]
        int? AlarmCount,
        [property: Key(30)] [property: Description("Comma-separated active alarm type names; null when AlarmCount is zero.")]
        [property: ListColumn(Order = 300)]
        string Alarms,
        [property: Key(31)] [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 320, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Key(32)] [property: Description("Hostname of the Jube node that took this sample.")]
        [property: ListColumn(Order = 310)]
        string Instance);
}
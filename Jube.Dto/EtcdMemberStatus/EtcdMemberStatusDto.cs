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

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.EtcdMemberStatus
{
    public sealed record EtcdMemberStatusDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this etcd member was sampled.")]
        DateTime OccurredDate,
        [property: Description("The host:port endpoint polled for this row (discovered or explicitly configured).")]
        string Endpoint,
        [property:
            Description(
                "This member's own etcd cluster member id (decimal, can exceed a 64-bit signed integer so kept as text).")]
        string MemberId,
        [property: Description("This member's configured name.")]
        string Name,
        [property: Description("Comma-separated peer URLs this member advertises to other members.")]
        string PeerUrls,
        [property: Description("Comma-separated client URLs this member advertises to clients.")]
        string ClientUrls,
        [property:
            Description(
                "Whether this member is a non-voting learner (added via member add --learner, not yet promoted).")]
        bool? IsLearner,
        [property: Description("This member's etcdserver version.")]
        string Version,
        [property: Description("The etcd cluster's negotiated protocol version, as reported by this member.")]
        string ClusterVersion,
        [property: Description("Whether this member reported itself healthy on GET /health at sample time.")]
        bool? HealthOk,
        [property: Description("The reason string GET /health returned when not healthy; null when healthy.")]
        string HealthReason,
        [property:
            Description(
                "The member id this node currently believes is the raft leader -- compare across every member's row in the same OccurredDate minute to spot disagreement (a split-brain symptom).")]
        string LeaderId,
        [property:
            Description(
                "Whether this member's own MemberId matches LeaderId -- true means this member believes itself to be the current leader.")]
        bool? IsLeader,
        [property:
            Description(
                "Whether this member currently has any leader at all (from the etcd_server_has_leader metric) -- false means this member is mid-election or partitioned from the rest of the cluster.")]
        bool? HasLeader,
        [property:
            Description(
                "Cumulative count of leader elections this member has observed since it started -- etcd's own counter, never reset by Jube; a climbing number across samples means the cluster is flapping leadership.")]
        long? LeaderChangesTotal,
        [property: Description("Total size of this member's backend database file, in bytes.")]
        long? DbSizeBytes,
        [property:
            Description(
                "Size of this member's backend database actually in use, in bytes -- the gap between this and DbSizeBytes is reclaimable via compaction+defrag.")]
        long? DbSizeInUseBytes,
        [property: Description("This member's raft log index.")]
        long? RaftIndex,
        [property:
            Description(
                "This member's current raft term -- a climbing term without a corresponding LeaderChangesTotal increase on other members can indicate a struggling/partitioned member repeatedly calling elections.")]
        long? RaftTerm,
        [property:
            Description(
                "The raft index this member has actually applied to its state machine -- a growing gap from RaftIndex means this member is falling behind applying committed entries.")]
        long? RaftAppliedIndex,
        [property:
            Description("Cumulative count of raft proposals committed, etcd's own counter, never reset by Jube.")]
        long? ProposalsCommittedTotal,
        [property: Description("Cumulative count of raft proposals applied, etcd's own counter, never reset by Jube.")]
        long? ProposalsAppliedTotal,
        [property:
            Description(
                "Number of raft proposals currently pending on this member -- a sustained non-zero value means this member (or the cluster) cannot keep up with the write rate.")]
        long? ProposalsPendingCount,
        [property:
            Description(
                "Cumulative count of raft proposals that failed, etcd's own counter, never reset by Jube -- any non-zero value is worth investigating.")]
        long? ProposalsFailedTotal,
        [property:
            Description(
                "Average WAL fsync latency in microseconds since this member started (etcd's own cumulative sum/count, not a per-minute figure) -- etcd's own documentation treats sustained values above roughly 10 milliseconds as a sign the disk cannot keep up.")]
        double? WalFsyncAvgMicroseconds,
        [property:
            Description(
                "Average backend (bolt db) commit latency in microseconds since this member started, same cumulative caveat as WalFsyncAvgMicroseconds.")]
        double? BackendCommitAvgMicroseconds,
        [property:
            Description(
                "Cumulative count of 'slow apply' warnings (applying a raft entry took too long), etcd's own counter, never reset by Jube.")]
        long? SlowApplyTotal,
        [property:
            Description(
                "Cumulative count of 'slow read index' warnings (a linearizable read took too long), etcd's own counter, never reset by Jube.")]
        long? SlowReadIndexesTotal,
        [property:
            Description(
                "Number of active etcd alarms on this member (e.g. NOSPACE when the backend quota is exceeded, CORRUPT when a data corruption is detected) -- any non-zero value here means etcd itself is refusing some operations.")]
        int? AlarmCount,
        [property: Description("Comma-separated active alarm type names; null when AlarmCount is zero.")]
        string Alarms,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that took this sample.")]
        string Instance);
}
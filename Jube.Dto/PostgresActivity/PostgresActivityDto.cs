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

namespace Jube.Dto.PostgresActivity
{
    public sealed record PostgresActivityDto(
        [property: Description("The backend process id (pg_stat_activity.pid) -- what you would pass to " +
                               "pg_cancel_backend/pg_terminate_backend in psql if this needs to be killed " +
                               "(this page is deliberately read-only and does not offer that action itself).")]
        int Pid,
        [property: Description("Database this backend is connected to.")]
        string? DatabaseName,
        [property: Description("Role name this backend authenticated as.")]
        string? UserName,
        [property:
            Description(
                "The application_name the connecting client set, when it set one -- Jube's own connections are not specially labelled, so this is however the connecting driver/tool identifies itself.")]
        string? ApplicationName,
        [property: Description("Client IP address, or null for a local/Unix-socket connection.")]
        string? ClientAddress,
        [property:
            Description(
                "What kind of backend this is: client backend (an ordinary connection), autovacuum worker/launcher, background writer, checkpointer, walwriter, walsender, logical replication worker, etc. A long-running autovacuum here is a common, otherwise invisible cause of perceived slowness.")]
        string? BackendType,
        [property:
            Description(
                "Backend's current state: active, idle, idle in transaction, idle in transaction (aborted), fastpath function call, disabled. 'idle in transaction' held for a long time (see TransactionDurationSeconds) is a classic connection-leak pattern -- it holds locks and blocks vacuum from cleaning up dead rows while doing nothing.")]
        string? State,
        [property:
            Description(
                "What this backend is actively waiting on right now, e.g. wait_event_type=Lock means it is blocked by another backend (see BlockedByPids); wait_event_type=IO means disk-bound; null means not waiting on anything. See PostgreSQL's own wait_event_type/wait_event documentation for the full list.")]
        string? WaitEventType,
        string? WaitEvent,
        [property:
            Description(
                "Process ids of the other backends currently blocking this one (from pg_blocking_pids()) -- empty when not blocked. This is the actual 'who is blocking whom' answer, not just 'this one is waiting'.")]
        int[] BlockedByPids,
        [property: Description("UTC timestamp this backend's connection was established.")]
        DateTime? BackendStartDate,
        [property: Description("UTC timestamp the current transaction began, or null if not currently in one.")]
        DateTime? TransactionStartDate,
        [property:
            Description(
                "Seconds since TransactionStartDate, or null if not in a transaction -- a large value alongside State='idle in transaction' is the connection-leak signature described above.")]
        double? TransactionDurationSeconds,
        [property: Description("UTC timestamp the current (or, if idle, the most recent) query started.")]
        DateTime? QueryStartDate,
        [property:
            Description(
                "Seconds since QueryStartDate while State is active -- how long the current query has actually been running.")]
        double? QueryDurationSeconds,
        [property:
            Description(
                "The current (or, if idle, the most recently completed) query's text. Visible here to the same extent Postgres' own role-based visibility rules allow for the role this connection authenticates as -- a non-superuser role without pg_read_all_stats only ever sees its own queries in full.")]
        string? Query);
}
/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.PostgresLock
{
    public sealed record PostgresLockDto(
        [property: Description("The backend process id holding or waiting on this lock (pg_locks.pid) -- " +
                               "cross-reference against PostgresActivityList's Pid for the rest of that " +
                               "backend's state.")]
        int Pid,
        [property: Description("Role name of the backend holding or waiting on this lock.")]
        string? UserName,
        [property: Description("That backend's current (or most recently completed) query text.")]
        string? Query,
        [property: Description("What kind of object this lock is on: relation, extend, page, tuple, " +
                               "transactionid, virtualxid, object, userlock, advisory. Most contention worth " +
                               "diagnosing is locktype='relation'.")]
        string? LockType,
        [property: Description("The specific lock mode held or wanted, e.g. AccessShareLock, RowExclusiveLock, " +
                               "ShareUpdateExclusiveLock, AccessExclusiveLock -- see PostgreSQL's own lock-mode " +
                               "compatibility table for which modes conflict with which.")]
        string? Mode,
        [property: Description("True if this lock is actually held; false means this backend is WAITING for " +
                               "it. A row with Granted=false is a backend stuck behind someone else -- find " +
                               "who by matching RelationName/LockType against a Granted=true row from a " +
                               "different Pid.")]
        bool Granted,
        [property: Description("The table/index/sequence this lock is on, when LockType is 'relation' or " +
                               "similar -- null for a lock that is not on a relation at all (an advisory " +
                               "lock, a transactionid lock, ...).")]
        string? RelationName,
        [property: Description("The virtual transaction id (backend/local-xid pair) this lock belongs to.")]
        string? VirtualTransactionId,
        [property: Description("True if this lock was satisfied by the fast path, skipping the shared lock " +
                               "manager -- normal for the common, uncontended case.")]
        bool FastPath);
}
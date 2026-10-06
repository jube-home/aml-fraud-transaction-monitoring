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

namespace Jube.Data.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Models;
    using Repository;

    public static class EntityApprovalResolver
    {
        public static int? EffectiveVersion(IEnumerable<EntityApprovalRow> approvals, int currentVersion,
            int approvalsRequired = 1)
        {
            ArgumentNullException.ThrowIfNull(approvals);

            if (approvalsRequired < 1)
            {
                approvalsRequired = 1;
            }

            int? effective = null;

            foreach (var byVersion in approvals.Where(w => w.EntityVersion <= currentVersion)
                         .GroupBy(g => g.EntityVersion))
            {
                if (effective.HasValue && byVersion.Key <= effective.Value)
                {
                    continue;
                }

                if (!IsApproved(byVersion, approvalsRequired))
                {
                    continue;
                }

                effective = byVersion.Key;
            }

            return effective;
        }

        public static Dictionary<int, int> EffectiveVersionsByEntity(IEnumerable<EntityApprovalRow> approvals,
            IReadOnlyDictionary<int, int> currentVersionByEntityId, int approvalsRequired = 1)
        {
            ArgumentNullException.ThrowIfNull(approvals);
            ArgumentNullException.ThrowIfNull(currentVersionByEntityId);

            var byEntity = approvals.GroupBy(g => g.EntityId)
                .ToDictionary(k => k.Key, v => v.ToList());

            var effective = new Dictionary<int, int>();

            foreach (var (entityId, currentVersion) in currentVersionByEntityId)
            {
                if (!byEntity.TryGetValue(entityId, out var rows))
                {
                    continue;
                }

                var version = EffectiveVersion(rows, currentVersion, approvalsRequired);

                if (version.HasValue)
                {
                    effective.Add(entityId, version.Value);
                }
            }

            return effective;
        }

        public static bool IsRejected(IEnumerable<EntityApprovalRow> approvals, int version)
        {
            ArgumentNullException.ThrowIfNull(approvals);

            return approvals.Any(a => a.EntityVersion == version && a.State == EntityApprovalState.Rejected);
        }

        public static bool IsPending(IEnumerable<EntityApprovalRow> approvals, int version,
            int approvalsRequired = 1)
        {
            ArgumentNullException.ThrowIfNull(approvals);

            var rows = approvals.Where(w => w.EntityVersion == version).ToList();

            return !IsApproved(rows, approvalsRequired)
                   && rows.All(a => a.State != EntityApprovalState.Rejected);
        }

        private static bool IsApproved(IEnumerable<EntityApprovalRow> versionApprovals, int approvalsRequired)
        {
            var approvers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var approval in versionApprovals)
            {
                if (approval.State == EntityApprovalState.Rejected)
                {
                    return false;
                }

                if (approval.State == EntityApprovalState.Approved)
                {
                    approvers.Add(approval.CreatedUser ?? string.Empty);
                }
            }

            return approvers.Count >= approvalsRequired;
        }
    }
}
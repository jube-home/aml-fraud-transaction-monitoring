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
using Jube.Dto.Interfaces;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.EntityAnalysisModelSuppression
{
    [FormEndpoint("EntityAnalysisModelSuppression")]
    [FormKeys(Id = nameof(Id), NaturalKey = nameof(SuppressionKeyValue))]
    public class EntityAnalysisModelSuppressionDto : IUpdated
    {
        [Description("Guid of the Model this Suppression applies to. Required; must reference an existing Model " +
                     "visible to the caller's tenant.")]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("The Request XPath name (flagged Enable Suppression) that this Suppression matches against, " +
                     "e.g. an IP address field. Required.")]
        public string? SuppressionKey { get; set; }

        [Description("The value of SuppressionKey to suppress on, e.g. a specific IP address. Required.")]
        public string? SuppressionKeyValue { get; set; }

        [Description("Wire-compatibility field carried from the legacy DTO. The presence of a (non-deleted, " +
                     "non-expired) row for the given EntityAnalysisModelGuid/SuppressionKey/SuppressionKeyValue " +
                     "IS the suppression state -- this flag has no backing column and is ignored by every " +
                     "operation. See the migration report for the toggle semantics of Update.")]
        public bool Active { get; set; }

        [Description("When set, the Suppression is automatically removed once this date/time (UTC) is reached, " +
                     "with no further synchronisation required. Must be in the future. Leave null for a " +
                     "Suppression that never expires.")]
        public DateTimeOffset? DeleteExpiryDate { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        public int Id { get; set; }

        [Description("Timestamp (UTC) this Suppression was created. Server-assigned. Read-only.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("No backing column -- always null when read back (pre-existing quirk, see migration " +
                     "report). Read-only.")]
        public string? UpdatedUser { get; set; }

        [Description("No backing column -- always null when read back (pre-existing quirk, see migration " +
                     "report). Read-only.")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("User who created this Suppression. Server-assigned. Read-only.")]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned optimistic-concurrency version number. Read-only.")]
        public int Version { get; set; }

        [Description("User who removed this Suppression, if soft-deleted. Server-assigned. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Suppression was removed, if applicable. Server-assigned. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
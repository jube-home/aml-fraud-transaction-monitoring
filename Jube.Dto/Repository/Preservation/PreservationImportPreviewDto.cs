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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.Preservation
{
    [Description("What importing a package or snapshot would do to the caller's tenant, worked out without changing " +
                 "anything. An import replaces the whole of the tenant's configuration, so the difference is the " +
                 "live tenant compared with what would replace it.")]
    public class PreservationImportPreviewDto
    {
        [Description("What would change, the live tenant on the left and the incoming configuration on the right.")]
        public PreservationDiffDto Diff { get; set; } = new();

        [Description("The format version of the incoming package or snapshot.")]
        public int PackageVersion { get; set; }

        [Description("The guid of the incoming package or snapshot body.")]
        public Guid PackageGuid { get; set; }

        [Description("Things the caller should know before importing that do not stop it.")]
        public List<PreservationImportWarningDto> Warnings { get; set; } = [];

        [Description("Things that would make the import fail. Empty when it can go ahead.")]
        public List<PreservationImportWarningDto> Blockers { get; set; } = [];

        [Description("The users the import would leave without a role, and what would happen to them.")]
        public PreservationOrphanedUsersDto OrphanedUsers { get; set; } = new();
    }
}

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

namespace Jube.Dto.Repository.PreservationSnapshot
{
    public class PreservationSnapshotRequestDto
    {
        [Description("Optional label for the snapshot, for example the reason for the change that follows it.")]
        public string? Name { get; set; }

        [Description("What is taking the snapshot. Defaults to the Preservation page.")]
        public PreservationSnapshotSource SnapshotSourceId { get; set; } = PreservationSnapshotSource.PreservationPage;

        [Description("Include Exhaustive Search Instances, which may be very large.")]
        public bool Exhaustive { get; set; }

        [Description("Include Lists and List Values.")]
        public bool Lists { get; set; } = true;

        [Description("Include Dictionaries and Dictionary Key Value Pairs.")]
        public bool Dictionaries { get; set; } = true;

        [Description("Include Visualisation Registry configuration.")]
        public bool Visualisations { get; set; } = true;

        [Description("Include roles and their permissions. On by default: a rollback removes what the " +
                     "snapshot body does not hold, so omitting roles loses the permission model.")]
        public bool Roles { get; set; } = true;

        [Description("Include Advanced Analytics instances, trained models and training history. On by " +
                     "default, for the same reason as Roles.")]
        public bool AdvancedAnalytics { get; set; } = true;
    }
}
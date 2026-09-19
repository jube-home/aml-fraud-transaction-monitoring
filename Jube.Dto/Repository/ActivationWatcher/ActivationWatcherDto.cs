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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Repository.ActivationWatcher
{
    public sealed record ActivationWatcherDto(
        [property: Description("Primary key of the ActivationWatcher row.")]
        int Id,
        [property: Description("Tenant that owns this activation event.")]
        int? TenantRegistryId,
        [property: Description("Name of the key/field that triggered the activation.")]
        string? Key,
        [property: Description("Value of the key/field that triggered the activation.")]
        string? KeyValue,
        [property: Description("Longitude associated with the activation, if geolocated.")]
        double? Longitude,
        [property: Description("Latitude associated with the activation, if geolocated.")]
        double? Latitude,
        [property: Description("Human-readable summary of the Activation Rule that fired.")]
        string? ActivationRuleSummary,
        [property: Description("Rendered content shown for the response elevation, if any.")]
        string? ResponseElevationContent,
        [property: Description("Numeric elevation of the response associated with the activation.")]
        double? ResponseElevation,
        [property: Description("Background colour used to render this activation on the watcher UI.")]
        string? BackColor,
        [property: Description("Foreground colour used to render this activation on the watcher UI.")]
        string? ForeColor,
        [property: Description("UTC timestamp the activation was created.")]
        DateTime? CreatedDate);
}
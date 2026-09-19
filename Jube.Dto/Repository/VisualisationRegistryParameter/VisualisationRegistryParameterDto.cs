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
namespace Jube.Dto.Repository.VisualisationRegistryParameter
{
    [FormEndpoint("VisualisationRegistryParameter")]
    [FormKeys(Id = nameof(Id), Parent = nameof(VisualisationRegistryId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Value", Order = 20)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class VisualisationRegistryParameterDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Visualisation Registry this Parameter belongs to. Set from the parent " +
                     "Visualisation Registry context; not user-editable.")]
        public int VisualisationRegistryId { get; set; }

        [Description("Display name of the Parameter, matched against the Case payload field of the same name " +
                     "when the Visualisation is embedded, and used as the named SQL parameter when running the " +
                     "report. Unique within the Visualisation Registry (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, the Parameter is available for collection and use in datasource SQL.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, the Parameter is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        public bool Locked { get; set; }

        [Description("The data type governing the control used to collect this Parameter and the type its " +
                     "Default value is interpreted as: 1 = String, 2 = Integer, 3 = Float, 4 = Date, " +
                     "5 = Boolean.")]
        [FormField(Group = "Value", Order = 10, Widget = "dropdown")]
        [ListColumn(Order = 30, Title = "Data Type")]
        public int DataTypeId { get; set; }

        [Description("The default value offered by the collection control and used to parameterise the report " +
                     "when a Case is not embedding a matching payload field. For a Date data type, this is an " +
                     "offset expressed as a number of days.")]
        [FormField(Group = "Value", Order = 20)]
        public string? DefaultValue { get; set; }

        [Description("When true, the Parameter must be supplied before the report can be run.")]
        [FormField(Group = "Value", Order = 30, Widget = "switch")]
        [ListColumn(Order = 40, Title = "Required")]
        public bool Required { get; set; }

        [Description("Server-assigned globally-unique identifier of this Parameter, referenced by Visualisation " +
                     "Registry Parameter Roles. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing Parameter.")]
        public int Id { get; set; }

        [Description("Server-assigned user who created this Parameter. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned timestamp (UTC) this Parameter was created. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who last updated this Parameter. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp (UTC) this Parameter was last updated. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this Parameter, if any. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp (UTC) this Parameter was soft-deleted, if any. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
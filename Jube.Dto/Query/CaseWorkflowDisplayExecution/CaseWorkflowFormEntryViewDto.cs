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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseWorkflowDisplayExecution
{
    [Description("A submitted form entry drawn read only from the form's current template, with its values filled in.")]
    [MessagePackObject]
    public class CaseWorkflowFormEntryViewDto
    {
        [Description("Server-assigned identifier of the entry.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Integer Id of the case the entry was submitted against.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description("Name of the form that was submitted.")]
        [Key(2)]
        public string FormName { get; set; } = string.Empty;

        [Description("User who submitted the entry.")]
        [Key(3)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When the entry was submitted.")]
        [Key(4)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("The escaped HTML fragment of the form with the submitted values filled in and every field " +
                     "disabled; empty when the template no longer compiles.")]
        [Key(5)]
        public string Html { get; set; } = string.Empty;

        [Description("Submitted values the form's current template has no field for, so they could not be drawn " +
                     "in the form; all of them when the template no longer compiles.")]
        [Key(6)]
        public List<CaseWorkflowFormEntryValueDto> OtherValues { get; set; } = [];

        [Description("When the template could not be drawn, why, in the page language.")]
        [Key(7)]
        public string? Error { get; set; }
    }
}

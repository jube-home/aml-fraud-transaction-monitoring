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

namespace Jube.Dto.Query.FormModel
{
    [Description("The declarative form model of one record type, read from the attributes on its DTO when the application starts: its fields, groups, list columns, keys and lock field.")]
    public class FormModelDto
    {
        [Description("The area name, as passed to the form endpoint attribute on the DTO; also the name this model is requested by.")]
        public string Area { get; set; } = string.Empty;

        [Description("The operations the record type supports, from list, read, create, update and delete; a form hides what is missing.")]
        public List<string> Operations { get; set; } = [];

        [Description("The API route the records of this type are created, read, updated and deleted through.")]
        public string Endpoint { get; set; } = string.Empty;

        [Description("How a checker approves records of this type; null when records of this type are not approved.")]
        public FormApprovalDto? Approval { get; set; }

        [Description("How roles are allocated to records of this type; null when they are not.")]
        public FormRolesDto? Roles { get; set; }

        [Description("The rows of values a record owns; null when it owns none.")]
        public FormValuesDto? Values { get; set; }

        [Description("Specialised editors that stand in for several of the properties below.")]
        public List<FormEditorDto> Editors { get; set; } = [];

        [Description("The names of the identifier, parent and natural key properties.")]
        public FormKeysDto Keys { get; set; } = new();

        [Description("Properties that lock the record when true; empty when the record cannot be locked.")]
        public List<string> LockFields { get; set; } = [];

        [Description("Field groups, in display order.")]
        public List<FormGroupDto> Groups { get; set; } = [];

        [Description("Form fields, in display order.")]
        public List<FormFieldDto> Fields { get; set; } = [];

        [Description("Columns of the list view, in display order.")]
        public List<FormListColumnDto> ListColumns { get; set; } = [];

        [Description("The kinds of record that belong under this one, in display order; empty when it has none.")]
        public List<FormChildDto> Children { get; set; } = [];

        public FormModelDto Copy()
        {
            return (FormModelDto)MemberwiseClone();
        }
    }
}

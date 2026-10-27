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

namespace Jube.Dto.Query.CaseWorkflowDisplayExecution
{
    [Description("A field the form template declares.")]
    public class CaseWorkflowFormFieldDto
    {
        [Description("The field name, as posted back with the submission.")]
        public string Name { get; set; } = string.Empty;

        [Description("The kind of field: text, number, date, dateTime, email, telephone, checkbox, radio, select or textArea.")]
        public string Kind { get; set; } = string.Empty;

        [Description("The allowed values of a select or radio group.")]
        public List<string> Options { get; set; } = [];
    }
}

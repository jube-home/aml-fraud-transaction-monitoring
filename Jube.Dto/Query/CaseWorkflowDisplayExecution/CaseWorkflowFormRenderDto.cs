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
    [Description("Request to render a case workflow form template against a case and the values entered so far.")]
    public class CaseWorkflowFormRenderDto
    {
        [Description("Integer Id of the case the form is filled in for.")]
        public int CaseId { get; set; }

        [Description("Integer Id of the active case workflow form whose template is rendered.")]
        public int CaseWorkflowFormId { get; set; }

        [Description("The values entered so far, keyed by field name. Fields that depend on them show or hide.")]
        public Dictionary<string, string?>? Values { get; set; }

        [Description("When true every field is drawn read-only, to show a submitted form.")]
        public bool ReadOnly { get; set; }
    }
}

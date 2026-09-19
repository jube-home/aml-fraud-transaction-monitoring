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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.CaseWorkflowDisplayExecution
{
    [Description("Request to render a case workflow display (an HTML template with payload tokens) against a case.")]
    public class CaseWorkflowDisplayExecutionDto
    {
        [Description("Integer Id of the case whose stored payload supplies the token values.")]
        public int CaseId { get; set; }

        [Description("Integer Id of the active case workflow display whose HTML template is rendered.")]
        public int CaseWorkflowDisplayId { get; set; }
    }
}
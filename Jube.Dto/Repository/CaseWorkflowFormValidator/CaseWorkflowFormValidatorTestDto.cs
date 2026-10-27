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

namespace Jube.Dto.Repository.CaseWorkflowFormValidator
{
    [Description("Request to try a saved Form validator against values, without submitting the Form.")]
    public class CaseWorkflowFormValidatorTestDto
    {
        [Description("Server-assigned identifier of the validator to try.")]
        public int Id { get; set; }

        [Description("Integer Id of a case supplying @CaseId and @CaseKeyValue; 0 for none.")]
        public int CaseId { get; set; }

        [Description("The values to try, keyed by field name.")]
        public Dictionary<string, string?>? Values { get; set; }
    }
}

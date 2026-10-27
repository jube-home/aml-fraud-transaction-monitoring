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
    [Description("The outcome of trying a Form validator against values.")]
    public class CaseWorkflowFormValidatorTestResultDto
    {
        [Description("True when the value passed the validator.")]
        public bool Passed { get; set; }

        [Description("When it failed, a short code: required, range, length, pattern, compare, list, dictionary, " +
                     "sql or sqlFailed (the SQL could not be run).")]
        public string? Code { get; set; }

        [Description("The standard or authored message shown for the failure, in the page language.")]
        public string? Message { get; set; }

        [Description("Milliseconds the check took.")]
        public int Milliseconds { get; set; }
    }
}

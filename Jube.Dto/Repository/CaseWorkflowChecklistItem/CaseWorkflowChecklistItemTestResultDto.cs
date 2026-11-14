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

namespace Jube.Dto.Repository.CaseWorkflowChecklistItem
{
    [Description("The outcome of trying a checklist item's SQL against a case.")]
    [MessagePackObject]
    public class CaseWorkflowChecklistItemTestResultDto
    {
        [Description("True when the SQL ran and returned boolean true. A blank condition is reported as true.")]
        [Key(0)]
        public bool Passed { get; set; }

        [Description("When it did not pass, a short code: sql (the SQL returned false) or sqlFailed (the SQL could " +
                     "not be run or timed out).")]
        [Key(1)]
        public string? Code { get; set; }

        [Description("The message shown for the failure, in the page language.")]
        [Key(2)]
        public string? Message { get; set; }

        [Description("Milliseconds the check took.")]
        [Key(3)]
        public int Milliseconds { get; set; }
    }
}

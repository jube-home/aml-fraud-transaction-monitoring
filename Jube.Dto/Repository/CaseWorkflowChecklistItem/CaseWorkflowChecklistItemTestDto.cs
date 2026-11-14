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
    [Description("Request to try a saved checklist item's show or validate SQL against a case, without ticking anything.")]
    [MessagePackObject]
    public class CaseWorkflowChecklistItemTestDto
    {
        [Description("Server-assigned identifier of the checklist item to try.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Which condition to try: 1 = the show SQL, 2 = the validate SQL.")]
        [Key(1)]
        public int ConditionId { get; set; }

        [Description("Integer Id of the case supplying @Id and @CaseKeyValue; 0 for none.")]
        [Key(2)]
        public int CaseId { get; set; }
    }
}

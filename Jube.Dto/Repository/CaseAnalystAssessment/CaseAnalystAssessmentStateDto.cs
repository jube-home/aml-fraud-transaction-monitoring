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
using System.Collections.Generic;
using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseAnalystAssessment
{
    [Description("The analyst assessment that stands on a case, and the ones recorded before it.")]
    [MessagePackObject]
    public class CaseAnalystAssessmentStateDto
    {
        [Description("The assessment that stands: the latest one not withdrawn. Empty when there is none.")]
        [Key(0)]
        public CaseAnalystAssessmentDto? Current { get; set; }

        [Description("Every assessment recorded on the case, newest first, withdrawn ones included.")]
        [Key(1)]
        public List<CaseAnalystAssessmentDto> History { get; set; } = [];
    }
}

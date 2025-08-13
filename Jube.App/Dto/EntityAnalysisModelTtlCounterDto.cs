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

using System;
using Jube.App.Dto.Interfaces;

namespace Jube.App.Dto
{
    public class EntityAnalysisModelTtlCounterDto : IUpdated
    {
        public int EntityAnalysisModelId { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public bool Locked { get; set; }
        public bool Active { get; set; }
        public int TtlCounterValue { get; set; }
        public string TtlCounterInterval { get; set; }
        public bool ResponsePayload { get; set; }
        public bool ReportTable { get; set; }
        public bool OnlineAggregation { get; set; }
        public string TtlCounterDataName { get; set; }
        public bool EnableLiveForever { get; set; }
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedUser { get; set; }
        public int Version { get; set; }
        public string DeletedUser { get; set; }
        public DateTime DeletedDate { get; set; }
    }
}
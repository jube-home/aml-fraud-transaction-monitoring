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
    public class EntityAnalysisModelRequestXPathDto : IUpdated
    {
        public int EntityAnalysisModelId { get; set; }
        public string Name { get; set; }
        public bool Locked { get; set; }
        public bool Active { get; set; }
        public int DataTypeId { get; set; }
        public bool SearchKey { get; set; }
        public string XPath { get; set; }
        public bool SearchKeyCache { get; set; }
        public int SearchKeyCacheValue { get; set; }
        public string SearchKeyCacheInterval { get; set; }
        public int SearchKeyCacheTtlValue { get; set; }
        public string SearchKeyCacheTtlInterval { get; set; }
        public bool EncryptOffDeploymentRegion { get; set; }
        public bool ResponsePayload { get; set; }
        public bool XPathExpression { get; set; }
        public bool HashEntityKeyComposite { get; set; }
        public bool HashEntryKeyComposite { get; set; }
        public bool SearchKeyCacheSample { get; set; }
        public int SearchKeyCacheFetchLimit { get; set; }
        public double Priority { get; set; }
        public bool EnableSuppression { get; set; }
        public bool ReportTable { get; set; }
        public string DefaultValue { get; set; }
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
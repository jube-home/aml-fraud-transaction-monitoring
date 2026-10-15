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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models
{
    using System.Collections.Generic;
    using Parser.Models;

    public class DistinctSearchKey
    {
        public readonly Dictionary<string, SelectedPayloadData> SelectedPayloadData = [];
        public int SearchKeyFetchLimit;
        public string SearchKeyTtlInterval;
        public int SearchKeyTtlIntervalValue;
        public string SqlSelect;
        public string SqlSelectFrom;
        public string SqlSelectOrderBy;
        public string SearchKeyCacheInterval { get; set; }
        public int SearchKeyCacheValue { get; set; }
        public bool SearchKeyCache { get; set; }
        public string SearchKeyCacheTtlInterval { get; set; }
        public int SearchKeyCacheTtlValue { get; set; }
        public int SearchKeyCacheFetchLimit { get; set; }
        public string SearchKey { get; set; }
        public bool SearchKeyCacheSample { get; set; }
    }
}
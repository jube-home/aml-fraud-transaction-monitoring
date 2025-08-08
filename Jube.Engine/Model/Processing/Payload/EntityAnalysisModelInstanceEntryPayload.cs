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
using System.Collections.Generic;
using Jube.Data.Poco;
using Jube.Engine.Model.Processing.CaseManagement;
using Newtonsoft.Json;

namespace Jube.Engine.Model.Processing.Payload;

public class EntityModelActivationRulePayload
{
    public bool Visible { get; set; }
}

public class EntityAnalysisModelInstanceEntryPayload
{
    [JsonProperty(Order = 1)] public ResponseElevation ResponseElevation { get; set; } = new();

    [JsonProperty(Order = 2)] public string EntityAnalysisModelInstanceName { get; set; }

    [JsonProperty(Order = 3)] public double R { get; set; }

    [JsonProperty(Order = 4)] public DateTime CreatedDate { get; set; }

    [JsonProperty(Order = 5)] public Guid EntityAnalysisModelGuid { get; set; }

    [JsonProperty(Order = 6)] public string EntityAnalysisModelName { get; set; }

    [JsonProperty(Order = 7)] public Guid EntityAnalysisModelInstanceGuid { get; set; }

    [JsonProperty(Order = 8)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }

    [JsonProperty(Order = 9)] public string EntityInstanceEntryId { get; set; }

    [JsonProperty(Order = 10)] public DateTime ReferenceDate { get; set; }

    [JsonProperty(Order = 11)] public bool Reprocess { get; set; }

    [JsonProperty(Order = 12)] public DateTime ArchiveEnqueueDate { get; set; }

    [JsonProperty(Order = 13)] public int? PrevailingEntityAnalysisModelActivationRuleId { get; set; }

    [JsonProperty(Order = 14)] public string PrevailingEntityAnalysisModelActivationRuleName { get; set; }

    [JsonProperty(Order = 15)] public int EntityAnalysisModelActivationRuleCount { get; set; }

    public Dictionary<string, object> Payload { get; set; }

    [JsonProperty(Order = 16)] public Dictionary<string, double> Dictionary { get; set; } = [];

    [JsonProperty(Order = 17)] public Dictionary<string, int> TtlCounter { get; } = [];

    [JsonProperty(Order = 18)] public Dictionary<string, double> Sanction { get; set; } = [];

    [JsonProperty(Order = 19)]
    public Dictionary<string, double> Abstraction { get; } =
        new();

    [JsonProperty(Order = 20)] public Dictionary<string, double> AbstractionCalculation { get; } = [];

    [JsonProperty(Order = 22)] public Dictionary<string, double> HttpAdaptation { get; } = [];

    [JsonProperty(Order = 23)] public Dictionary<string, double> ExhaustiveAdaptation { get; } = [];

    [JsonProperty(Order = 24)] public Dictionary<string, EntityModelActivationRulePayload> Activation { get; } = [];

    [JsonProperty(Order = 25)] public CreateCase CreateCasePayload { get; set; }

    [JsonProperty(Order = 26)] public Dictionary<string, double> Tag { get; } = [];

    [JsonProperty(Order = 27)] public Dictionary<string, int> ResponseTime { get; } = [];

    [JsonIgnore] public List<ArchiveKey> ReportDatabaseValues { get; set; } = [];

    [JsonIgnore] public bool StoreInRdbms { get; set; }

    [JsonIgnore] public int EntityAnalysisModelId { get; set; }

    [JsonIgnore] public int TenantRegistryId { get; set; }
}
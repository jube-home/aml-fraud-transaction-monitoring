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

namespace Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery
{
    public class GetNextExhaustiveSearchInstanceQueryDto
    {
        public int Id { get; set; }
        public int EntityAnalysisModelId { get; set; }
        public int TenantRegistryId { get; set; }
        public string FilterJson { get; set; }
        public string FilterTokens { get; set; }
        public string FilterSql { get; set; }
        public bool Anomaly { get; set; }
        public double AnomalyProbability { get; set; }
        public int? AnomalyAlgorithmId { get; set; }
        public string AnomalyParametersJson { get; set; }
        public bool Filter { get; set; }
        public string CategoricalOptionsJson { get; set; }
        public byte DataSourceId { get; set; }
        public int? ExhaustiveInstanceCsvId { get; set; }
        public int? ExhaustiveInstanceJsonId { get; set; }
        public double? BaseRate { get; set; }
        public string CalibrationMethod { get; set; }
        public bool Bootstrap { get; set; }
        public int? BootstrapTo { get; set; }
        public double? BootstrapWeightFactor { get; set; }
        public double? ChallengerModelBaseProbability { get; set; }
    }
}
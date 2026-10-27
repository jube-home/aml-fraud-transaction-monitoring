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
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class CaseWorkflowVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(3)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(4)] public int? CaseStatusId { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(12)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(14)] public int? VisualisationRegistryId { get; set; }
        [Column] [Nullable] [Key(15)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(16)] public byte? EnableVisualisation { get; set; }
        [Column] [Nullable] [Key(17)] public int? Version { get; set; }
        [Column] [Nullable] [Key(18)] public string ValueField { get; set; }
        [Column] [Key(19)] public byte TypologySeverityId { get; set; }
        [Column] [Nullable] [Key(20)] public int? FirstViewTargetMinutes { get; set; }
        [Column] [Nullable] [Key(21)] public int? OwnershipTargetMinutes { get; set; }
        [Column] [Nullable] [Key(22)] public int? ResolutionTargetMinutes { get; set; }
        [Column] [Nullable] [Key(23)] public double? RiskKappa { get; set; }
        [Column] [Nullable] [Key(24)] public double? RiskRho { get; set; }
        [Column] [Nullable] [Key(25)] public double? RiskPriorStrength { get; set; }
        [Column] [Nullable] [Key(26)] public double? RiskBaseRatePrior { get; set; }
        [Column] [Nullable] [Key(27)] public int? RiskDeadlineLaneDays { get; set; }
        [Column] [Nullable] [Key(28)] public double? RiskKeyHistoryHalfLifeDays { get; set; }
        [Column] [Nullable] [Key(29)] public string RiskImpactWeights { get; set; }
        [Column] [Nullable] [Key(30)] public string RiskMoneyBands { get; set; }
        [Column] [Nullable] [Key(31)] public double? RiskQaSampleRate { get; set; }
        [Column] [Nullable] [Key(32)] public int? DailyGoal { get; set; }
    }
}
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
using LinqToDB;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelActivationRuleVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelActivationRuleId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string BuilderRuleScript { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(4)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(5)] public string Name { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public double? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(8)] public Guid CaseWorkflowGuid { get; set; }
        [Column] [Nullable] [Key(9)] public int? EnableCaseWorkflow { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(11)] public int? Version { get; set; }
        [Column] [Nullable] [Key(12)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(14)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(15)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(17)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(18)] public Guid EntityAnalysisModelTtlCounterGuid { get; set; }
        [Column] [Nullable] [Key(19)] public Guid EntityAnalysisModelGuidTtlCounter { get; set; }
        [Column] [Nullable] [Key(20)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(21)] public byte? EnableTtlCounter { get; set; }
        [Column] [Nullable] [Key(22)] public string ResponseElevationContent { get; set; }
        [Column] [Nullable] [Key(23)] public byte? SendToActivationWatcher { get; set; }
        [Column] [Nullable] [Key(24)] public string ResponseElevationForeColor { get; set; }
        [Column] [Nullable] [Key(25)] public string ResponseElevationBackColor { get; set; }
        [Column] [Nullable] [Key(26)] public Guid CaseWorkflowStatusGuid { get; set; }
        [Column] [Nullable] [Key(27)] public double? ActivationSample { get; set; }
        [Column] [Nullable] [Key(28)] public long? ActivationCounter { get; set; }
        [Column] [Nullable] [Key(29)] public DateTime? ActivationCounterDate { get; set; }
        [Column] [Nullable] [Key(30)] public string ResponseElevationRedirect { get; set; }
        [Column] [Nullable] [Key(32)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(33)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(34)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(35)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(36)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(37)] public string NotificationBody { get; set; }
        [Column] [Nullable] [Key(38)] public string CoderRuleScript { get; set; }
        [Column] [Nullable] [Key(39)] public byte? RuleScriptTypeId { get; set; }
        [Column] [Nullable] [Key(40)] public byte? EnableResponseElevation { get; set; }
        [Column] [Nullable] [Key(41)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(42)] public string ResponseElevationKey { get; set; }
        [Column] [Nullable] [Key(43)] public byte? EnableBypass { get; set; }
        [Column] [Nullable] [Key(44)] public char? BypassSuspendInterval { get; set; }
        [Column] [Nullable] [Key(45)] public int? BypassSuspendValue { get; set; }
        [Column] [Nullable] [Key(46)] public double? BypassSuspendSample { get; set; }
        [Column] [Nullable] [Key(47)] public byte? Visible { get; set; }
        [Column] [Nullable] [Key(48)] public byte? EnableReprocessing { get; set; }
        [Column] [Nullable] [Key(49)] public byte? EnableOverride { get; set; }
        [Column] [Nullable] [Key(50)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(51)] public double? Priority { get; set; }
        [Column] [Nullable] [Key(52)] public long? EvaluationCounter { get; set; }
        [Column] [Nullable] [Key(53)] public byte? EnableForce { get; set; }
        [Column] [Nullable] [Key(54)] public string OverrideKey { get; set; }
    }
}
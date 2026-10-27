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
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class EntityAnalysisModel
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(4)] public string EntryXPath { get; set; }
        [Column] [Nullable] [Key(5)] public string ReferenceDateXPath { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(14)] public string EntryName { get; set; }
        [Column] [Nullable] [Key(15)] public string ReferenceDateName { get; set; }
        [Column] [Nullable] [Key(16)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(17)] public int? CacheFetchLimit { get; set; }
        [Column] [Nullable] [Key(18)] public char? CacheTtlInterval { get; set; }
        [Column] [Nullable] [Key(19)] public int? CacheTtlIntervalValue { get; set; }
        [Column] [Nullable] [Key(20)] public byte? ReferenceDatePayloadLocationTypeId { get; set; }
        [Column] [Nullable] [Key(21)] public double? MaxResponseElevation { get; set; }
        [Column] [Nullable] [Key(22)] public char? MaxResponseElevationInterval { get; set; }
        [Column] [Nullable] [Key(23)] public int? MaxResponseElevationValue { get; set; }
        [Column] [Nullable] [Key(24)] public int? MaxResponseElevationThreshold { get; set; }
        [Column] [Nullable] [Key(25)] public char? MaxActivationWatcherInterval { get; set; }
        [Column] [Nullable] [Key(26)] public int? MaxActivationWatcherValue { get; set; }
        [Column] [Nullable] [Key(27)] public int? MaxActivationWatcherThreshold { get; set; }
        [Column] [Nullable] [Key(28)] public double? ActivationWatcherSample { get; set; }
        [Column] [Nullable] [Key(29)] public byte? EnableActivationArchive { get; set; }
        [Column] [Nullable] [Key(30)] public byte? EnableCache { get; set; }
        [Column] [Nullable] [Key(31)] public byte? EnableTtlCounter { get; set; }
        [Column] [Nullable] [Key(32)] public byte? EnableSanctionCache { get; set; }
        [Column] [Nullable] [Key(33)] public byte? EnableRdbmsArchive { get; set; }
        [Column] [Nullable] [Key(34)] public byte? EnableActivationWatcher { get; set; }
        [Column] [Nullable] [Key(35)] public byte? EnableResponseElevationLimit { get; set; }
        [Column] [Nullable] [Key(36)] public int? Version { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(37)]
        public IEnumerable<EntityAnalysisModelVersion> EntityAnalysisModelVersion { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(38)]
        public IEnumerable<EntityAnalysisModelAbstractionCalculation>
            EntityAnalysisModelAbstractionCalculation { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(39)]
        public IEnumerable<EntityAnalysisModelAbstractionRule> EntityAnalysisModelAbstractionRule { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(40)]
        public IEnumerable<EntityAnalysisModelActivationRule> EntityAnalysisModelActivationRule { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(41)]
        public IEnumerable<EntityAnalysisModelHttpAdaptation> EntityAnalysisModelHttpAdaptation { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(42)]
        public IEnumerable<EntityAnalysisModelDictionary> EntityAnalysisModelDictionary { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(43)]
        public IEnumerable<EntityAnalysisModelGatewayRule> EntityAnalysisModelGatewayRule { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(44)]
        public IEnumerable<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunction { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(45)]
        public IEnumerable<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScript { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(46)]
        public IEnumerable<EntityAnalysisModelList> EntityAnalysisModelList { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(47)]
        public IEnumerable<EntityAnalysisModelRequestXpath> EntityAnalysisModelRequestXpath { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(48)]
        public IEnumerable<EntityAnalysisModelSanction> EntityAnalysisModelSanction { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(49)]
        public IEnumerable<EntityAnalysisModelTtlCounter> EntityAnalysisModelTtlCounter { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(50)]
        public IEnumerable<EntityAnalysisModelTag> EntityAnalysisModelTag { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(69)]
        public IEnumerable<EntityAnalysisModelResponseElevationRange> EntityAnalysisModelResponseElevationRange { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(51)]
        public IEnumerable<ExhaustiveSearchInstance> ExhaustiveSearchInstance { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(52)]
        public IEnumerable<CaseWorkflow> CaseWorkflow { get; set; }

        [Association(ThisKey = "TenantRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(53)]
        public TenantRegistry TenantRegistry { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(54)]
        public IEnumerable<EntityAnalysisModelSuppression> EntityAnalysisModelSuppression { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(55)]
        public IEnumerable<EntityAnalysisModelActivationRuleSuppression> EntityAnalysisModelActivationRuleSuppression
        {
            get;
            set;
        }

        [Column] [Nullable] [Key(56)] public int? ImportId { get; set; }
        [Column] [Nullable] [Key(57)] public byte? EnableImplicitAsync { get; set; }
        [Column] [Nullable] [Key(58)] public int? ImplicitAsyncTimeoutMilliseconds { get; set; }
        [Column] [Nullable] [Key(59)] public byte? EnableTrace { get; set; }
        [Column] [Nullable] [Key(60)] public byte? EnableLogsInfo { get; set; }
        [Column] [Nullable] [Key(61)] public int? LogsWarnThresholdMilliseconds { get; set; }
        [Column] [Nullable] [Key(62)] public byte? EnableLogsInResponse { get; set; }
        [Column] [Nullable] [Key(63)] public byte? EnableSampling { get; set; }
        [Column] [Nullable] [Key(64)] public double? SamplePercentage { get; set; }
        [Column] [Nullable] [Key(65)] public byte? EnableLogsWarnThreshold { get; set; }
        [Column] [Nullable] [Key(66)] public byte? EnableLogs { get; set; }

        [Association(ThisKey = "Guid", OtherKey = "EntityAnalysisModelGuid", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(67)]
        public IEnumerable<EntityAnalysisModelOverride> EntityAnalysisModelOverride { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(68)]
        public IEnumerable<EntityAnalysisModelActivationRuleOverride> EntityAnalysisModelActivationRuleOverride
        {
            get;
            set;
        }
    }
}
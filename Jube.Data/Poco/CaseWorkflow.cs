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
    public class CaseWorkflow
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public int? CaseStatusId { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public Guid VisualisationRegistryGuid { get; set; }
        [Column] [Nullable] [Key(14)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(15)] public byte? EnableVisualisation { get; set; }
        [Column] [Nullable] [Key(16)] public int? Version { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(17)]
        public IEnumerable<CaseWorkflowAction> CaseWorkflowAction { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(18)]
        public IEnumerable<CaseWorkflowDisplay> CaseWorkflowDisplay { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(19)]
        public IEnumerable<CaseWorkflowFilter> CaseWorkflowFilter { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(20)]
        public IEnumerable<CaseWorkflowForm> CaseWorkflowForm { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(21)]
        public IEnumerable<CaseWorkflowMacro> CaseWorkflowMacro { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(22)]
        public IEnumerable<CaseWorkflowStatus> CaseWorkflowStatus { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(23)]
        public IEnumerable<CaseWorkflowXPath> CaseWorkflowXPath { get; set; }

        [Association(ThisKey = "TenantRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(24)]
        public TenantRegistry TenantRegistry { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(25)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(26)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(27)] public int? ImportId { get; set; }

        [Association(ThisKey = "Guid", OtherKey = "CaseWorkflowGuid", CanBeNull = false,
            Relationship = Relationship.OneToMany)]
        [Key(28)]
        public CaseWorkflowRole CaseWorkflowRole { get; set; }
    }
}
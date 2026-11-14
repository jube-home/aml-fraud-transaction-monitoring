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
    public class CaseAgentReplay
    {
        [Column] [PrimaryKey] [Identity] [Key(0)] public long Id { get; set; }
        [Column] [Key(1)] public Guid Guid { get; set; }
        [Column] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Key(3)] public short StatusId { get; set; }
        [Column] [Nullable] [Key(4)] public string PromptText { get; set; }
        [Column] [Key(5)] public string AgentDefinitionHash { get; set; }
        [Column] [Nullable] [Key(6)] public string ModelName { get; set; }
        [Column] [Key(7)] public DateTime FromDate { get; set; }
        [Column] [Key(8)] public DateTime ToDate { get; set; }
        [Column] [Key(9)] public int MaximumCases { get; set; }
        [Column] [Key(10)] public int AsOfMinutes { get; set; }
        [Column] [Nullable] [Key(11)] public Guid? CaseWorkflowGuid { get; set; }
        [Column] [Key(12)] public int CasesSelected { get; set; }
        [Column] [Key(13)] public int CasesScored { get; set; }
        [Column] [Nullable] [Key(14)] public string Error { get; set; }
        [Column] [Key(15)] public string CreatedUser { get; set; }
        [Column] [Key(16)] public DateTime CreatedDate { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? StartedDate { get; set; }
        [Column] [Nullable] [Key(18)] public DateTime? CompletedDate { get; set; }
    }
}

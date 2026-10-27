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

using Jube.Data.Repository.Models;
using System;
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class AdvancedAnalyticsTrainingRunStep
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Key(1)] public int TenantRegistryId { get; set; }
        [Column] [Key(2)] public int AskJoobyAnalyticsInstanceId { get; set; }
        [Column] [Key(3)] public int AdvancedAnalyticsTrainingRunId { get; set; }
        [Column] [Key(4)] public int StepOrder { get; set; }
        [Column] [Key(5)] public AdvancedAnalyticsStep Step { get; set; }
        [Column] [Key(6)] public AdvancedAnalyticsStatus StatusId { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? StartedDate { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(9)] public double? DurationMilliseconds { get; set; }
        [Column] [Nullable] [Key(10)] public string Error { get; set; }
    }
}
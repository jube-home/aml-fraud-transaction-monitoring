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

using System.ComponentModel;
using Jube.Dto.Validation;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.EntityAnalysisModelBacktest
{
    [Description("Why one archived transaction was or was not counted: whether the filter kept it, whether the " +
                 "rule fired, whether it is positive, and the values they read.")]
    [MessagePackObject]
    public class BacktestExplanationDto
    {
        [Description("True when the request compiled and the transaction was found.")]
        [Key(0)]
        public bool Valid { get; set; }

        [Description("The transaction.")] [Key(1)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [Description("Its entry key value.")] [Key(2)] public string? EntryKeyValue { get; set; }
        [Description("Its reference date.")] [Key(3)] public DateTime? ReferenceDate { get; set; }
        [Description("Its live tags.")] [Key(4)] public List<string> Tags { get; set; } = [];

        [Description("Whether the filter kept it.")]
        [Key(5)]
        public bool PassedFilter { get; set; }

        [Description("The error the filter raised, if any.")]
        [Key(6)]
        public string? FilterError { get; set; }

        [Description("Whether the rule fired.")]
        [Key(7)]
        public bool Fired { get; set; }

        [Description("The error the rule raised, if any.")]
        [Key(8)]
        public string? RuleError { get; set; }

        [Description("Whether it is in the positive class; null when no class was given.")]
        [Key(9)]
        public bool? Positive { get; set; }

        [Description("The values the rule and filter read, by completion name.")]
        [Key(10)]
        public List<BacktestValueDto> Values { get; set; } = [];

        [Description("Why it could not be explained.")]
        [Key(11)]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}
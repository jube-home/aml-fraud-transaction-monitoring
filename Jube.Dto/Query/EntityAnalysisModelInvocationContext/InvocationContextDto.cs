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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.EntityAnalysisModelInvocationContext
{
    [Description("A context for evaluating rules as the invocation pipeline would: every name a rule can use in " +
                 "the Entity Analysis Model (by completion name, e.g. Payload.Amount or Abstraction.CountLastHour) " +
                 "with its value, where the value came from, and which pipeline stages were computed, taken from " +
                 "the archive, left for an overlay, or excluded because they store or notify.")]
    [MessagePackObject]
    public class InvocationContextDto
    {
        [Description("Id of the Entity Analysis Model the context belongs to.")]
        [Key(0)]
        public int EntityAnalysisModelId { get; set; }

        [Description("How the context was built: Blank, RequestJson or Archive.")]
        [Key(1)]
        public string Source { get; set; } = string.Empty;

        [Description("The reference date the pipeline would use, in UTC; null for a blank context.")]
        [Key(2)]
        public DateTime? ReferenceDate { get; set; }

        [Description("The entry id (the model's entry XPath value), when known.")]
        [Key(3)]
        public string? EntryId { get; set; }

        [Description("Guid of the archived transaction the context was read from, for an Archive context.")]
        [Key(4)]
        public Guid? EntityAnalysisModelInstanceEntryGuid { get; set; }

        [Description("Every name a rule can use in this model, ordered by name, with its value.")]
        [Key(5)]
        public List<InvocationContextValueDto> Values { get; set; } = [];

        [Description("The invocation pipeline stages in order, with what the context knows about each.")]
        [Key(6)]
        public List<InvocationContextStageDto> Stages { get; set; } = [];

        [Description("Problems found while applying an overlay, e.g. an unknown name or a value of the wrong " +
                     "type. Empty when there were none.")]
        [Key(7)]
        public List<string> Errors { get; set; } = [];
    }
}
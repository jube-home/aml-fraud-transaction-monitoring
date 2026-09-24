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

using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;

namespace Jube.Service.Query.EntityAnalysisModelInvocationContext
{
    internal static class EntityAnalysisModelInvocationContextMapper
    {
        internal static InvocationContextDto ToDto(InvocationContext context, IEnumerable<string>? errors = null)
        {
            return new InvocationContextDto
            {
                EntityAnalysisModelId = context.EntityAnalysisModelId,
                Source = context.Source.ToString(),
                ReferenceDate = context.ReferenceDate,
                EntryId = context.EntryId,
                EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryGuid,
                Values = context.Values.Values.Select(v => new InvocationContextValueDto
                {
                    Name = v.Field.Name,
                    Group = v.Field.Group,
                    DataType = v.Field.DataType,
                    Value = InvocationContextBuilder.FormatValue(v.Value),
                    Origin = v.Origin.ToString(),
                    Note = v.Note
                }).ToList(),
                Stages = context.Stages.Select(s => new InvocationContextStageDto
                {
                    Stage = s.Stage,
                    Status = s.Status.ToString(),
                    Note = s.Note
                }).ToList(),
                Errors = errors?.ToList() ?? []
            };
        }

        internal static InvocationContext ToContext(InvocationContextDto dto)
        {
            var context = new InvocationContext
            {
                EntityAnalysisModelId = dto.EntityAnalysisModelId,
                Source = Enum.TryParse<InvocationContextSource>(dto.Source, out var source)
                    ? source
                    : InvocationContextSource.Blank,
                ReferenceDate = dto.ReferenceDate,
                EntryId = dto.EntryId,
                EntityAnalysisModelInstanceEntryGuid = dto.EntityAnalysisModelInstanceEntryGuid
            };

            foreach (var value in dto.Values.Where(v => !string.IsNullOrEmpty(v.Name) && v.Name.Contains('.')))
            {
                var field = new InvocationContextField(value.Name, value.Group, value.DataType);
                object? typed = null;
                if (value.Value != null && InvocationContextBuilder.TryConvertText(value.DataType, value.Value,
                        out var converted))
                {
                    typed = converted;
                }

                context.Values[value.Name] = new InvocationValue(field, typed,
                    Enum.TryParse<InvocationValueOrigin>(value.Origin, out var origin)
                        ? origin
                        : InvocationValueOrigin.Unset, value.Note);
            }

            foreach (var stage in dto.Stages)
            {
                context.Stages.Add(new InvocationStage(stage.Stage,
                    Enum.TryParse<InvocationStageStatus>(stage.Status, out var status)
                        ? status
                        : InvocationStageStatus.NotComputed, stage.Note));
            }

            return context;
        }
    }
}
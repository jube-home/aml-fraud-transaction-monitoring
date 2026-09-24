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

namespace Jube.Engine.EntityAnalysisModelInvoke.Simulation
{
    using System;
    using System.Collections.Generic;

    public sealed record InvocationContextField(string Name, string Group, string DataType)
    {
        public string Namespace => Name[..Name.IndexOf('.')];
        public string Key => Name[(Name.IndexOf('.') + 1)..];
    }

    public sealed record InvocationValue(
        InvocationContextField Field,
        object Value,
        InvocationValueOrigin Origin,
        string Note = null);

    public sealed record InvocationStage(string Stage, InvocationStageStatus Status, string Note);

    public sealed class InvocationContext
    {
        public int EntityAnalysisModelId { get; init; }
        public InvocationContextSource Source { get; init; }
        public DateTime? ReferenceDate { get; set; }
        public string EntryId { get; set; }
        public Guid? EntityAnalysisModelInstanceEntryGuid { get; init; }
        public SortedDictionary<string, InvocationValue> Values { get; } = new(StringComparer.Ordinal);
        public List<InvocationStage> Stages { get; } = [];
    }
}
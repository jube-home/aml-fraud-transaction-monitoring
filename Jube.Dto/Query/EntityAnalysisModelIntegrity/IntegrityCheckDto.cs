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

namespace Jube.Dto.Query.EntityAnalysisModelIntegrity
{
    [Description("One finding of a model integrity check, with a stable code.")]
    [MessagePackObject]
    public class IntegrityCheckDto
    {
        [Description("The check's stable code, e.g. DependencyDangling or EngineCompileFailed.")]
        [Key(0)]
        public string Code { get; set; } = string.Empty;

        [Description("A short description of the check, e.g. Uses a name that does not exist.")]
        [Key(1)]
        public string Title { get; set; } = string.Empty;

        [Description("Error (something does not work), Warning (something may not work as intended) or Info.")]
        [Key(2)]
        public string Severity { get; set; } = string.Empty;

        [Description("Dependencies, Compilation, Configuration or Engine.")]
        [Key(3)]
        public string Category { get; set; } = string.Empty;

        [Description("The kind of entity the finding is about, when it is about one.")]
        [Key(4)]
        public string? EntityKind { get; set; }

        [Description("The entity's id.")] [Key(5)] public int? EntityId { get; set; }

        [Description("The entity's name, or the engine instance for an engine finding.")]
        [Key(6)]
        public string? EntityName { get; set; }

        [Description("What was found and what it means.")]
        [Key(7)]
        public string Message { get; set; } = string.Empty;
    }
}
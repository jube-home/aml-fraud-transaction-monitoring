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

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Query.EntityAnalysisModelIntegrity
{
    [Description("One finding of a model integrity check, with a stable code.")]
    public class IntegrityCheckDto
    {
        [Description("The check's stable code, e.g. DependencyDangling or EngineCompileFailed.")]
        public string Code { get; set; } = string.Empty;

        [Description("A short description of the check, e.g. Uses a name that does not exist.")]
        public string Title { get; set; } = string.Empty;

        [Description("Error (something does not work), Warning (something may not work as intended) or Info.")]
        public string Severity { get; set; } = string.Empty;

        [Description("Dependencies, Compilation, Configuration or Engine.")]
        public string Category { get; set; } = string.Empty;

        [Description("The kind of entity the finding is about, when it is about one.")]
        public string? EntityKind { get; set; }

        [Description("The entity's id.")] public int? EntityId { get; set; }

        [Description("The entity's name, or the engine instance for an engine finding.")]
        public string? EntityName { get; set; }

        [Description("What was found and what it means.")]
        public string Message { get; set; } = string.Empty;
    }
}
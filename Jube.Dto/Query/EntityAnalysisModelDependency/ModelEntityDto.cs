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

namespace Jube.Dto.Query.EntityAnalysisModelDependency
{
    [Description("An entity of a model that can depend on, or be depended on by, others.")]
    [MessagePackObject]
    public class ModelEntityDto
    {
        [Description("The kind of entity, e.g. ActivationRule, TtlCounter, List or RequestXPath.")]
        [Key(0)]
        public string Kind { get; set; } = string.Empty;

        [Description("The entity's id.")] [Key(1)] public int Id { get; set; }
        [Description("The entity's name.")] [Key(2)] public string Name { get; set; } = string.Empty;

        [Description("Whether the entity is active.")]
        [Key(3)]
        public bool Active { get; set; }
    }
}
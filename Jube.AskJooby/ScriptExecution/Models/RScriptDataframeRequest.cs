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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.AskJooby.ScriptExecution.Models
{
    public sealed record RScriptDataframeRequest(
        [property: Description(
            "The name the R script will see this as -- a data.frame bound under this exact name in the " +
            "script's own environment before it runs. Must be a valid R identifier.")]
        string Name,
        [property: Description(
            "A pointer returned earlier by a caching tool (for example CacheAssembledTable or a SQL query " +
            "tool), identifying the rows to hydrate into this data.frame. Only the \"tbl_...\" cache pointer " +
            "shape is accepted here.")]
        string Pointer);
}
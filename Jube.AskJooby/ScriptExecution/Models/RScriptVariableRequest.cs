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
    public sealed record RScriptVariableRequest(
        [property: Description(
            "The name the R script will see this as -- a plain base-type variable bound under this exact " +
            "name in the script's own environment before it runs. Must be a valid R identifier.")]
        string Name,
        [property: Description(
            "The value to bind. Its own JSON shape decides the R type it becomes: a number becomes R " +
            "numeric, a string becomes character, true/false becomes logical, and an array becomes a " +
            "vector of whichever of those its elements are. Do not send null -- there is no single R type " +
            "a null could mean; omit the variable instead.")]
        object? Value);
}
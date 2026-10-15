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

namespace Jube.Dto.Query.ExhaustiveSearchInstanceVariable
{
    [Description("Reallocates a trained variable's live name and processing type, without retraining. Recall " +
                 "resolves variables by name and processing type only, so this changes what live field the " +
                 "network's fixed weight position reads at the next recall.")]
    public sealed class ExhaustiveSearchInstanceVariableReallocateDto
    {
        [Description(
            "New bare variable name, starting with a letter and containing only letters, digits and underscores, " +
            "up to 64 characters. The processing type's prefix (e.g. Payload.) is added automatically and must " +
            "not be included here.")]
        public string? Name { get; set; }

        [Description("New processing type: Payload (1), Dictionary (2), TtlCounter (3), Sanction (4), " +
                     "Abstraction (5) or AbstractionCalculation (6).")]
        public int ProcessingTypeId { get; set; }
    }
}
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

namespace Jube.Dto.Options
{
    using System.ComponentModel;

    public enum CaseSimilarityKind
    {
        [Description("Rule")] Rule = 1,
        [Description("Model")] Model = 2,
        [Description("Tag")] Tag = 3,
        [Description("Entity")] Entity = 4,
        [Description("Category")] Category = 5,
        [Description("Measure")] Measure = 6,
        [Description("Feature")] Feature = 7,
        [Description("Band")] Band = 8
    }
}

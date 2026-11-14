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

    public enum CaseWorkflowFormValidatorKind
    {
        [Description("Required")] Required = 1,
        [Description("Required when another field equals a value")] RequiredWhen = 2,
        [Description("Range (numbers or dates)")] Range = 3,
        [Description("Length (characters)")] Length = 4,
        [Description("Pattern (regular expression)")] Pattern = 5,
        [Description("Compare with another field")] Compare = 6,
        [Description("In a model list")] InList = 7,
        [Description("In a model dictionary")] InDictionary = 8,
        [Description("SQL (must return true)")] Sql = 9
    }
}

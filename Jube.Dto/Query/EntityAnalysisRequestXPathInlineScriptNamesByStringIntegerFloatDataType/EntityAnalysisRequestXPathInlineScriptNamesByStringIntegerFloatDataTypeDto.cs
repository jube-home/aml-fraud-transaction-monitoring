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
namespace Jube.Dto.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType
{
    public class EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto
    {
        [Description("Name of the model field that can be referenced by a rule as a String or Float search key: " +
                     "a request XPath, an Inline Script public property or an Inline Function.")]
        public string? Name { get; set; }

        [Description("Data type of the field: 1 String, 3 Float.")]
        public int DataTypeId { get; set; }

        [Description("True when the field is flagged as a search key.")]
        public bool SearchKey { get; set; }
    }
}
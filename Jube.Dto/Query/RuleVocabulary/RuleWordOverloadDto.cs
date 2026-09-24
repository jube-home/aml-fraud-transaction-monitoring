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

namespace Jube.Dto.Query.RuleVocabulary
{
    [Description("One way to call a rule function.")]
    public class RuleWordOverloadDto
    {
        [Description("The call as Visual Basic, e.g. <String>.IsMatch(pattern As String) As Boolean, where " +
                     "<String> is the value it is called on.")]
        public string Signature { get; set; } = string.Empty;

        [Description("The type of value it is called on, e.g. String or Double.")]
        public string ReceiverType { get; set; } = string.Empty;

        [Description("The type it returns.")] public string ReturnType { get; set; } = string.Empty;

        [Description("Its parameters after the value it is called on.")]
        public List<RuleWordParameterDto> Parameters { get; set; } = [];
    }
}
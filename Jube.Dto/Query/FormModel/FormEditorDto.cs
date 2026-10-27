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

namespace Jube.Dto.Query.FormModel
{
    [Description("A specialised editor that edits several properties of a record as one component, such as the rule " +
                 "builder and coder, which stands in for the properties it names.")]
    public class FormEditorDto
    {
        [Description("Which editor to use: ruleBuilder, caseFilter, exhaustiveDataset, exhaustiveFilter or datasource.")]
        public string Kind { get; set; } = string.Empty;

        [Description("The rule parse type, for the rule builder; null for other editors.")]
        public int? ParseType { get; set; }

        [Description("The property that each role of the editor reads and writes, in camel case. Every property " +
                     "named here is edited by the component, so a form shows the component once, in place of them.")]
        public Dictionary<string, string> Roles { get; set; } = [];

        [Description("The properties the editor reads for context but does not edit, in camel case, such as the " +
                     "data source an Exhaustive filter depends on. They stay ordinary form fields.")]
        public Dictionary<string, string> Reads { get; set; } = [];
    }
}

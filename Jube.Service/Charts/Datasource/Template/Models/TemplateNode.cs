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


// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Charts.Datasource.Template.Models
{
    public enum TemplateBranch
    {
        None,
        If,
        ElseIf,
        Else
    }

    public abstract record TemplateNode(int Line, int Column);

    public sealed record TemplateTextNode(string Text, int Line, int Column) : TemplateNode(Line, Column);

    public sealed record TemplateInterpolationNode(TemplateExpression Expression, int Line, int Column)
        : TemplateNode(Line, Column);

    public sealed record TemplateAttributePart(string? Text, TemplateExpression? Expression);

    public sealed record TemplateAttribute(
        string Name,
        string? Static,
        TemplateExpression? Bound,
        IReadOnlyList<TemplateAttributePart>? Parts = null);

    public sealed record TemplateLoop(string ItemName, string? IndexName, TemplateExpression Source);

    public sealed record TemplateElementNode(
        string Tag,
        IReadOnlyList<TemplateAttribute> Attributes,
        IReadOnlyList<TemplateNode> Children,
        TemplateBranch Branch,
        TemplateExpression? Condition,
        TemplateLoop? Loop,
        int Line,
        int Column) : TemplateNode(Line, Column);
}

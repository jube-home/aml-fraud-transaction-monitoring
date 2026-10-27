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
    public abstract record TemplateExpression;

    public sealed record TemplateLiteral(object? Value) : TemplateExpression;

    public sealed record TemplateName(string Name) : TemplateExpression;

    public sealed record TemplateMember(TemplateExpression Target, TemplateExpression Key) : TemplateExpression;

    public sealed record TemplateUnary(string Operator, TemplateExpression Operand) : TemplateExpression;

    public sealed record TemplateBinary(string Operator, TemplateExpression Left, TemplateExpression Right)
        : TemplateExpression;

    public sealed record TemplateConditional(
        TemplateExpression Test,
        TemplateExpression WhenTrue,
        TemplateExpression WhenFalse) : TemplateExpression;

    public sealed record TemplateFilterCall(
        TemplateExpression Input,
        string Name,
        IReadOnlyList<TemplateExpression> Arguments) : TemplateExpression;
}

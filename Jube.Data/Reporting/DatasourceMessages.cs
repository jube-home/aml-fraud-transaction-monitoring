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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jube.Data.Repository.Exceptions;

namespace Jube.Data.Reporting
{
    public static class DatasourceMessages
    {
        public const string ColumnMissing = "ColumnMissing";
        public const string ColumnNotATime = "ColumnNotATime";
        public const string KindUnknown = "KindUnknown";
        public const string RoleNotUsed = "RoleNotUsed";
        public const string RoleTakesOne = "RoleTakesOne";
        public const string OverrideColumnMissing = "OverrideColumnMissing";
        public const string OverrideColumnType = "OverrideColumnType";
        public const string RoleUnsatisfied = "RoleUnsatisfied";
        public const string PivotDuplicate = "PivotDuplicate";
        public const string HeatmapDuplicate = "HeatmapDuplicate";
        public const string NoRows = "NoRows";
        public const string LimitExceeded = "LimitExceeded";
        public const string SeriesNeedsOneValue = "SeriesNeedsOneValue";
        public const string SeriesNeedsOneVertical = "SeriesNeedsOneVertical";
        public const string MapNeedsBoth = "MapNeedsBoth";
        public const string MapNeedsPlace = "MapNeedsPlace";
        public const string MapEither = "MapEither";
        public const string MapNeedsValue = "MapNeedsValue";
        public const string GaugeNeedsRow = "GaugeNeedsRow";
        public const string CardEmpty = "CardEmpty";
        public const string CardFailed = "CardFailed";
        public const string MappingColumnUnknown = "MappingColumnUnknown";
        public const string MappingEntryEmpty = "MappingEntryEmpty";
        public const string ParameterEmpty = "ParameterEmpty";
        public const string ParameterDuplicate = "ParameterDuplicate";
        public const string RoleUndefined = "RoleUndefined";
        public const string RoleAssignedTwice = "RoleAssignedTwice";
        public const string SqlEmpty = "SqlEmpty";
        public const string ColumnDuplicate = "ColumnDuplicate";
        public const string ColumnsUnnamed = "ColumnsUnnamed";
        public const string ParameterForeign = "ParameterForeign";
        public const string ParameterValue = "ParameterValue";
        public const string SqlSelectOnly = "SqlSelectOnly";
        public const string SqlRejected = "SqlRejected";
        public const string SqlRejectedHint = "SqlRejectedHint";
        public const string SqlRejectedGeneric = "SqlRejectedGeneric";
        public const string TemplateEmpty = "TemplateEmpty";
        public const string TemplateTooLong = "TemplateTooLong";
        public const string TemplateUnclosedTag = "TemplateUnclosedTag";
        public const string TemplateUnexpectedClose = "TemplateUnexpectedClose";
        public const string TemplateTagNotAllowed = "TemplateTagNotAllowed";
        public const string TemplateAttributeNotAllowed = "TemplateAttributeNotAllowed";
        public const string TemplateDirectiveInvalid = "TemplateDirectiveInvalid";
        public const string TemplateLoopInvalid = "TemplateLoopInvalid";
        public const string TemplateExpressionInvalid = "TemplateExpressionInvalid";
        public const string TemplateNameUnknown = "TemplateNameUnknown";
        public const string TemplateFilterUnknown = "TemplateFilterUnknown";
        public const string TemplateFilterArguments = "TemplateFilterArguments";
        public const string TemplateInterpolationOpen = "TemplateInterpolationOpen";
        public const string TemplateTooDeep = "TemplateTooDeep";
        public const string TemplateTooLarge = "TemplateTooLarge";
        public const string TemplateTooManySteps = "TemplateTooManySteps";
        public const string FormFieldNameMissing = "FormFieldNameMissing";
        public const string FormFieldNameInvalid = "FormFieldNameInvalid";
        public const string FormFieldNameDuplicate = "FormFieldNameDuplicate";
        public const string FormFieldInLoop = "FormFieldInLoop";
        public const string FormInputTypeInvalid = "FormInputTypeInvalid";
        public const string FormOptionsFixed = "FormOptionsFixed";
        public const string FormLabelTargetUnknown = "FormLabelTargetUnknown";

        private static readonly Dictionary<string, string> english = new(StringComparer.Ordinal)
        {
            ["ColumnMissing"] = "The result has no column named \"{0}\".",
            ["ColumnNotATime"] = "The column \"{0}\" holds a value that is not a date and time.",
            ["KindUnknown"] = "There is no chart of kind {0}.",
            ["RoleNotUsed"] = "The {0} does not use the role {1}.",
            ["RoleTakesOne"] = "The role {0} takes one column, not {1}.",
            ["OverrideColumnMissing"] = "The result has no column named \"{0}\" for the role {1}.",
            ["OverrideColumnType"] = "The column \"{0}\" cannot fill the role {1}: it needs {2}.",
            ["RoleUnsatisfied"] = "The {0} needs a column for the role {1}, but no unused {2} is left in the result.",
            ["PivotDuplicate"] = "The result has more than one row for \"{0}\" in \"{1}\". Aggregate it in SQL so each pair appears once.",
            ["HeatmapDuplicate"] = "The result has more than one row for \"{0}\" and \"{1}\". Aggregate it in SQL so each pair appears once.",
            ["NoRows"] = "The query returned no rows, so there is nothing to draw.",
            ["LimitExceeded"] = "The {0} cannot draw this much data ({1} against a limit of {2} for {3}). Aggregate the query in SQL, or filter it down.",
            ["SeriesNeedsOneValue"] = "When rows are split into series by a column, exactly one value column is drawn.",
            ["SeriesNeedsOneVertical"] = "When rows are split into series by a column, exactly one vertical column is drawn.",
            ["MapNeedsBoth"] = "A map of points needs both a latitude and a longitude column.",
            ["MapNeedsPlace"] = "A map needs either a latitude and a longitude column, or a country column. Name them in the mapping if their names are not obvious.",
            ["MapEither"] = "A map plots either points or countries, not both.",
            ["MapNeedsValue"] = "A map of countries needs a numeric value column to colour them by.",
            ["GaugeNeedsRow"] = "The gauge needs a row to read its value from.",
            ["CardEmpty"] = "The query returned no rows.",
            ["CardFailed"] = "The card could not be produced.",
            ["MappingColumnUnknown"] = "The mapping names the column \"{0}\", which the SQL command does not return.",
            ["MappingEntryEmpty"] = "A mapping entry is empty.",
            ["ParameterEmpty"] = "A parameter is empty.",
            ["ParameterDuplicate"] = "A parameter is supplied more than once.",
            ["RoleUndefined"] = "There is no chart role {0}.",
            ["RoleAssignedTwice"] = "The role {0} is assigned twice.",
            ["SqlEmpty"] = "The SQL command is empty.",
            ["ColumnDuplicate"] = "The column name \"{0}\" appears more than once. Give each column its own alias.",
            ["ColumnsUnnamed"] = "More than one column has no name. Give each column an alias, for example: AS \"Total\".",
            ["ParameterForeign"] = "The parameter {0} does not belong to this visualisation registry.",
            ["ParameterValue"] = "The value for \"{0}\" must be {1}.",
            ["SqlSelectOnly"] = "The SQL command must be a single SELECT statement that uses no blocked statement, function or table.",
            ["SqlRejected"] = "The reporting database rejected the SQL command: {0} ({1}).",
            ["SqlRejectedHint"] = "The reporting database rejected the SQL command: {0} ({1}). {2}",
            ["SqlRejectedGeneric"] = "The SQL command was rejected by the reporting database.",
            ["TemplateEmpty"] = "The template is empty.",
            ["TemplateTooLong"] = "The template is longer than the limit of {0} characters.",
            ["TemplateUnclosedTag"] = "Template line {0}, column {1}: the tag <{2}> is never closed.",
            ["TemplateUnexpectedClose"] = "Template line {0}, column {1}: the closing tag </{2}> has no matching opening tag.",
            ["TemplateTagNotAllowed"] = "Template line {0}, column {1}: the tag <{2}> is not allowed.",
            ["TemplateAttributeNotAllowed"] = "Template line {0}, column {1}: the attribute \"{2}\" is not allowed on <{3}>.",
            ["TemplateDirectiveInvalid"] = "Template line {0}, column {1}: the directive \"{2}\" is not valid here.",
            ["TemplateLoopInvalid"] = "Template line {0}, column {1}: the v-for \"{2}\" must look like \"row in rows\" or \"(row, index) in rows\".",
            ["TemplateExpressionInvalid"] = "Template line {0}, column {1}: the expression \"{2}\" is not valid near \"{3}\".",
            ["TemplateNameUnknown"] = "Template line {0}, column {1}: \"{2}\" is not known here. Use rows, columns, rowCount, truncated or a loop variable.",
            ["TemplateFilterUnknown"] = "Template line {0}, column {1}: the filter \"{2}\" does not exist.",
            ["TemplateFilterArguments"] = "Template line {0}, column {1}: the filter \"{2}\" does not take that many arguments.",
            ["TemplateInterpolationOpen"] = "Template line {0}, column {1}: an opening [@ is never closed with @].",
            ["TemplateTooDeep"] = "Template line {0}, column {1}: the template nests deeper than {2} levels.",
            ["TemplateTooLarge"] = "The template produced more than {0} characters of output. Narrow the rows in SQL.",
            ["TemplateTooManySteps"] = "The template needed more than {0} steps to render. Narrow the rows in SQL or loop less.",
            ["FormFieldNameMissing"] = "Template line {0}, column {1}: the <{2}> needs a name.",
            ["FormFieldNameInvalid"] = "Template line {0}, column {1}: the field name \"{2}\" must start with a letter and use only letters, digits and underscores.",
            ["FormFieldNameDuplicate"] = "Template line {0}, column {1}: the field name \"{2}\" is used more than once.",
            ["FormFieldInLoop"] = "Template line {0}, column {1}: a <{2}> field cannot be inside a v-for, because a field name must be fixed.",
            ["FormInputTypeInvalid"] = "Template line {0}, column {1}: the input type \"{2}\" is not allowed.",
            ["FormOptionsFixed"] = "Template line {0}, column {1}: the field \"{2}\" needs fixed options, each with a plain value and none inside a v-for.",
            ["FormLabelTargetUnknown"] = "Template line {0}, column {1}: the label is for \"{2}\", which is not a field in this form.",
        };

        public static readonly MessageTerm NeedsNumeric = new("Term.Needs.Numeric", "a numeric column");
        public static readonly MessageTerm NeedsTime = new("Term.Needs.Time", "a date and time column");
        public static readonly MessageTerm NeedsNumericOrTime = new("Term.Needs.NumericOrTime", "a numeric or date and time column");
        public static readonly MessageTerm NeedsAny = new("Term.Needs.Any", "a column");
        public static readonly MessageTerm NounNumeric = new("Term.Noun.Numeric", "numeric column");
        public static readonly MessageTerm NounTime = new("Term.Noun.Time", "date and time column");
        public static readonly MessageTerm NounNumericOrTime = new("Term.Noun.NumericOrTime", "numeric or date and time column");
        public static readonly MessageTerm NounAny = new("Term.Noun.Any", "column");
        public static readonly MessageTerm ExpectedWholeNumber = new("Term.Expected.WholeNumber", "a whole number");
        public static readonly MessageTerm ExpectedNumber = new("Term.Expected.Number", "a number");
        public static readonly MessageTerm ExpectedDateTime = new("Term.Expected.DateTime", "an ISO 8601 date and time");
        public static readonly MessageTerm ExpectedFlag = new("Term.Expected.Flag", "1 or 0");

        public static IReadOnlyDictionary<string, string> English => english;

        public static IReadOnlyList<MessageTerm> Terms { get; } =
        [
            NeedsNumeric,
            NeedsTime,
            NeedsNumericOrTime,
            NeedsAny,
            NounNumeric,
            NounTime,
            NounNumericOrTime,
            NounAny,
            ExpectedWholeNumber,
            ExpectedNumber,
            ExpectedDateTime,
            ExpectedFlag
        ];

        public static string Format(string code, IReadOnlyList<object> arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, english[code], [.. arguments.Select(a => a?.ToString() ?? string.Empty)]);
        }
    }
}

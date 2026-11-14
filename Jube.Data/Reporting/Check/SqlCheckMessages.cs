/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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

namespace Jube.Data.Reporting.Check
{
    public static class SqlCheckMessages
    {
        public const string SyntaxError = "SqlSyntaxError";
        public const string UnknownTable = "SqlUnknownTable";
        public const string UnknownTableSuggest = "SqlUnknownTableSuggest";
        public const string UnknownColumn = "SqlUnknownColumn";
        public const string UnknownColumnSuggest = "SqlUnknownColumnSuggest";
        public const string UnknownAlias = "SqlUnknownAlias";
        public const string UnknownAliasSuggest = "SqlUnknownAliasSuggest";
        public const string AmbiguousColumn = "SqlAmbiguousColumn";
        public const string UnknownFunction = "SqlUnknownFunction";
        public const string UnknownFunctionSuggest = "SqlUnknownFunctionSuggest";
        public const string FunctionArity = "SqlFunctionArity";
        public const string OverNotWindow = "SqlOverNotWindow";
        public const string WindowNeedsOver = "SqlWindowNeedsOver";
        public const string WindowNotAllowed = "SqlWindowNotAllowed";
        public const string AggregateInWhere = "SqlAggregateInWhere";
        public const string AggregateInJoin = "SqlAggregateInJoin";
        public const string AggregateInGroupBy = "SqlAggregateInGroupBy";
        public const string AggregateNested = "SqlAggregateNested";
        public const string ColumnNotGrouped = "SqlColumnNotGrouped";
        public const string TypeMismatch = "SqlTypeMismatch";
        public const string LiteralInvalid = "SqlLiteralInvalid";
        public const string FunctionArgumentType = "SqlFunctionArgumentType";
        public const string AggregateArgumentType = "SqlAggregateArgumentType";
        public const string OperatorType = "SqlOperatorType";
        public const string ConditionType = "SqlConditionType";
        public const string TypesUnmatched = "SqlTypesUnmatched";
        public const string LimitNegative = "SqlLimitNegative";
        public const string LimitType = "SqlLimitType";
        public const string LimitVariables = "SqlLimitVariables";
        public const string OrdinalOutOfRange = "SqlOrdinalOutOfRange";
        public const string SetOperationColumns = "SqlSetOperationColumns";
        public const string SubqueryColumns = "SqlSubqueryColumns";
        public const string UnknownType = "SqlUnknownType";
        public const string ParameterUnknown = "SqlParameterUnknown";
        public const string ParameterType = "SqlParameterType";

        private static readonly Dictionary<string, string> english = new(StringComparer.Ordinal)
        {
            [SyntaxError] = "The SQL cannot be read near \"{0}\".",
            [UnknownTable] = "There is no table \"{0}\" that you may query.",
            [UnknownTableSuggest] = "There is no table \"{0}\" that you may query. Did you mean \"{1}\"?",
            [UnknownColumn] = "There is no column \"{0}\" in this query.",
            [UnknownColumnSuggest] = "There is no column \"{0}\" in this query. Did you mean \"{1}\"? Names with capital letters must be in double quotes.",
            [UnknownAlias] = "There is no table or alias named \"{0}\" in this query.",
            [UnknownAliasSuggest] = "There is no table or alias named \"{0}\" in this query. Did you mean \"{1}\"?",
            [AmbiguousColumn] = "The column \"{0}\" is ambiguous. Put a table name or alias in front of it.",
            [UnknownFunction] = "There is no function \"{0}\" with that name.",
            [UnknownFunctionSuggest] = "There is no function \"{0}\" with that name. Did you mean \"{1}\"?",
            [FunctionArity] = "The function \"{0}\" cannot be given {1} arguments. It takes {2}.",
            [OverNotWindow] = "OVER cannot be used with \"{0}\" because it is not an aggregate or window function.",
            [WindowNeedsOver] = "The window function \"{0}\" needs an OVER clause.",
            [WindowNotAllowed] = "The window function \"{0}\" is not allowed in this part of the query.",
            [AggregateInWhere] = "The aggregate function \"{0}\" is not allowed in WHERE. Filter on aggregates in HAVING instead.",
            [AggregateInJoin] = "The aggregate function \"{0}\" is not allowed in a JOIN condition.",
            [AggregateInGroupBy] = "The aggregate function \"{0}\" is not allowed in GROUP BY.",
            [AggregateNested] = "Aggregate functions cannot be nested. \"{0}\" is used inside another aggregate.",
            [ColumnNotGrouped] = "The column \"{0}\" must appear in GROUP BY or be used inside an aggregate function.",
            [TypeMismatch] = "A value of type {0} cannot be compared or combined with a value of type {1}.",
            [LiteralInvalid] = "The value '{0}' is not valid for the type {1}.",
            [FunctionArgumentType] = "The function \"{0}\" does not accept a value of type {1}.",
            [AggregateArgumentType] = "The aggregate function \"{0}\" does not accept a value of type {1}.",
            [OperatorType] = "The operator {0} cannot be used with a value of type {1}.",
            [ConditionType] = "A condition must be true or false, not a value of type {0}.",
            [TypesUnmatched] = "Values of type {0} and {1} cannot be combined into one result.",
            [LimitNegative] = "{0} cannot be negative.",
            [LimitType] = "{0} needs a whole number, not a value of type {1}.",
            [LimitVariables] = "{0} cannot refer to columns.",
            [OrdinalOutOfRange] = "{0} position {1} is not in the select list.",
            [SetOperationColumns] = "Each query in a UNION, INTERSECT or EXCEPT must return the same number of columns, but these return {0} and {1}.",
            [SubqueryColumns] = "A subquery used as a value must return exactly one column.",
            [UnknownType] = "There is no data type \"{0}\".",
            [ParameterUnknown] = "The parameter @{0} is not available here.",
            [ParameterType] = "The parameter @{0} holds a value of type {1} but is compared with a column of type {2}."
        };

        public static IReadOnlyDictionary<string, string> English => english;

        public static bool IsKnown(string code)
        {
            return code != null && english.ContainsKey(code);
        }

        public static string Format(string code, IReadOnlyList<object> arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, english[code],
                [.. arguments.Select(a => a?.ToString() ?? string.Empty)]);
        }
    }
}
